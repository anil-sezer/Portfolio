#!/usr/bin/env python3
"""
Parses Go test -json output files across cron microservices and generates
a formatted Markdown summary appended to GitHub Actions GITHUB_STEP_SUMMARY.
"""

import glob
import json
import os
import sys

# Ensure UTF-8 output even on Windows consoles with limited codepages (e.g. cp1252)
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
if hasattr(sys.stderr, "reconfigure"):
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")


def main():
    search_pattern = os.environ.get("GO_TEST_PATTERN", "kubernetes/crons/**/test-results.json")
    json_files = glob.glob(search_pattern, recursive=True)

    summary_file = os.environ.get("GITHUB_STEP_SUMMARY")

    if not json_files:
        msg = "### 🐹 Go Test Results\n\nNo `test-results.json` files found.\n"
        print(msg)
        if summary_file:
            with open(summary_file, "a", encoding="utf-8") as f:
                f.write(msg)
        return

    total_tests = 0
    total_passed = 0
    total_failed = 0
    total_skipped = 0
    total_duration_sec = 0.0

    modules = {}
    failed_tests = []

    for file_path in sorted(json_files):
        # Extract meaningful cron directory name from path
        # e.g., kubernetes/crons/ip-lookup-cron/test-results.json -> ip-lookup-cron
        dir_name = os.path.basename(os.path.dirname(file_path))
        module_name = dir_name if dir_name else "cron"

        mod_tests = {}
        mod_output = {}
        mod_duration = 0.0

        try:
            with open(file_path, "r", encoding="utf-8") as f:
                for line in f:
                    line = line.strip()
                    if not line:
                        continue
                    try:
                        ev = json.loads(line)
                    except json.JSONDecodeError:
                        continue

                    test_name = ev.get("Test")
                    action = ev.get("Action")
                    pkg = ev.get("Package", module_name)

                    if test_name:
                        if action == "output":
                            out_text = ev.get("Output", "")
                            mod_output.setdefault(test_name, []).append(out_text)
                        elif action in ("pass", "fail", "skip"):
                            elapsed = float(ev.get("Elapsed", 0.0))
                            mod_tests[test_name] = {
                                "action": action,
                                "elapsed": elapsed,
                                "package": pkg,
                            }
                    elif action in ("pass", "fail") and "Elapsed" in ev:
                        mod_duration += float(ev.get("Elapsed", 0.0))
        except Exception as e:
            print(f"Warning: Failed reading {file_path}: {e}", file=sys.stderr)
            continue

        m_passed = sum(1 for t in mod_tests.values() if t["action"] == "pass")
        m_failed = sum(1 for t in mod_tests.values() if t["action"] == "fail")
        m_skipped = sum(1 for t in mod_tests.values() if t["action"] == "skip")
        m_total = len(mod_tests)

        total_tests += m_total
        total_passed += m_passed
        total_failed += m_failed
        total_skipped += m_skipped
        total_duration_sec += mod_duration

        for t_name, t_data in mod_tests.items():
            if t_data["action"] == "fail":
                out_lines = "".join(mod_output.get(t_name, []))
                failed_tests.append({
                    "module": module_name,
                    "package": t_data["package"],
                    "test": t_name,
                    "output": out_lines,
                })

        modules[module_name] = {
            "total": m_total,
            "passed": m_passed,
            "failed": m_failed,
            "skipped": m_skipped,
            "duration": mod_duration,
        }

    status_icon = "🟢" if total_failed == 0 else "🔴"
    status_label = "Passed" if total_failed == 0 else "Failed"

    lines = []
    lines.append(f"## {status_icon} Go Test Results: {status_label}\n")
    lines.append("| Metric | Count |")
    lines.append("| :--- | :---: |")
    lines.append(f"| **Total Tests** | {total_tests} |")
    lines.append(f"| 🟢 **Passed** | {total_passed} |")
    lines.append(f"| 🔴 **Failed** | {total_failed} |")
    lines.append(f"| 🟡 **Skipped** | {total_skipped} |")
    lines.append(f"| ⏱️ **Duration** | {total_duration_sec:.2f}s |\n")

    if modules:
        lines.append("### 📦 Cron Modules Breakdown\n")
        lines.append("| Cron Module | Tests | Passed | Failed | Skipped | Duration | Outcome |")
        lines.append("| :--- | :---: | :---: | :---: | :---: | :---: | :---: |")
        for name, data in sorted(modules.items()):
            icon = "✅" if data["failed"] == 0 else "❌"
            lines.append(
                f"| `{name}` | {data['total']} | {data['passed']} | "
                f"{data['failed']} | {data['skipped']} | {data['duration']:.2f}s | {icon} |"
            )
        lines.append("")

    if failed_tests:
        lines.append("### ❌ Failed Test Details\n")
        for f in failed_tests:
            lines.append(f"<details><summary><b>[{f['module']}] {f['test']}</b></summary>\n")
            if f["output"]:
                lines.append(f"**Output:**\n```text\n{f['output'].strip()}\n```\n")
            lines.append("</details>\n")

    markdown = "\n".join(lines) + "\n"
    print(markdown)

    if summary_file:
        try:
            with open(summary_file, "a", encoding="utf-8") as sf:
                sf.write(markdown)
            print(f"Successfully appended summary to {summary_file}")
        except Exception as e:
            print(f"Warning: Failed to write to {summary_file}: {e}", file=sys.stderr)


if __name__ == "__main__":
    main()
