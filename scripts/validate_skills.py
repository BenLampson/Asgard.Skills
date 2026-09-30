"""Validate manifests, routing links, and current API/template invariants.

Requires PyYAML. Run with UTF-8 enabled on Windows: python -X utf8 scripts/validate_skills.py
This checks static invariants; it does not measure model invocation reliability.
"""

import re
import sys
from pathlib import Path

import yaml

ROOT = Path(__file__).resolve().parents[1]
errors = []
description_chars = 0
skills = sorted((ROOT / "skills").iterdir())
names = {folder.name for folder in skills if (folder / "SKILL.md").is_file()}
# Production operations are provided separately from this development repository.
external_skills = {"heimdall-production-release"}

for folder in skills:
    manifest = folder / "SKILL.md"
    if not manifest.is_file():
        continue
    text = manifest.read_text(encoding="utf-8")
    match = re.match(r"\A---\n(.*?)\n---(?:\n|$)", text, re.S)
    if not match:
        errors.append(f"{manifest}: missing YAML frontmatter")
        continue
    try:
        metadata = yaml.safe_load(match[1])
    except yaml.YAMLError as exc:
        errors.append(f"{manifest}: invalid YAML: {exc}")
        continue
    if not isinstance(metadata, dict):
        errors.append(f"{manifest}: frontmatter must be a mapping")
        continue
    name = metadata.get("name", "")
    description = metadata.get("description", "")
    if not isinstance(name, str) or name != folder.name or not re.fullmatch(r"[a-z0-9]+(?:-[a-z0-9]+)*", name) or len(name) > 64:
        errors.append(f"{manifest}: invalid or mismatched name")
    if not isinstance(description, str) or not description.strip() or len(description) > 1024 or any(c in description for c in "<>"):
        errors.append(f"{manifest}: invalid description")
    else:
        description_chars += len(description)
        if len(description) > 180:
            errors.append(f"{manifest}: description exceeds repository discovery budget (180 characters)")
    agent = folder / "agents" / "openai.yaml"
    if agent.exists():
        try:
            config = yaml.safe_load(agent.read_text(encoding="utf-8"))
            if not isinstance(config, dict):
                raise ValueError("agent configuration must be a mapping")
            policy = config.get("policy", {})
            if "allow_implicit_invocation" in policy and not isinstance(policy["allow_implicit_invocation"], bool):
                raise ValueError("allow_implicit_invocation must be boolean")
        except (yaml.YAMLError, ValueError, TypeError) as exc:
            errors.append(f"{agent}: {exc}")

    for source in folder.rglob("*.md"):
        content = source.read_text(encoding="utf-8")
        for target in re.findall(r"\]\(([^)]+)\)", content):
            if re.match(r"[a-zA-Z][a-zA-Z0-9+.-]*:|#", target):
                continue
            path = target.split("#", 1)[0]
            if path and not (source.parent / path).exists():
                errors.append(f"{source}: missing link target {target}")
        for skill in re.findall(r"\$((?:asgard-|heimdall-|dotnet-|identity-)[a-z0-9-]+)", content):
            if skill not in names | external_skills:
                errors.append(f"{source}: unknown skill {skill}")

    for source in [manifest, *folder.glob("templates/*")]:
        if not source.is_file():
            continue
        content = source.read_text(encoding="utf-8")
        if re.search(r"public\s+static\s+extension\s+\w", content):
            errors.append(f"{source}: obsolete extension block syntax")
        if folder.name != "asgard-cache" and re.search(r"\b(?:IMultiLevelCache|AddMultiLevelCache)\b", content):
            errors.append(f"{source}: legacy cache API in current guidance/template")

if errors:
    print("\n".join(errors))
    sys.exit(1)
print(f"Validated {len(names)} skills; total description length: {description_chars} characters.")
