#!/usr/bin/env python3
"""
Parses MSTest/VSTest .trx result files and generates a formatted Markdown
summary appended to GitHub Actions GITHUB_STEP_SUMMARY.
"""

import glob
import os
import sys
import xml.etree.ElementTree as ET

# Ensure UTF-8 output even on Windows consoles with limited codepages (e.g. cp1252)
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
if hasattr(sys.stderr, "reconfigure"):
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")


def parse_duration_to_seconds(duration_str):
    if not duration_str:
        return 0.0
    try:
        parts = duration_str.split(":")
        if len(parts) == 3:
            hours = float(parts[0])
            minutes = float(parts[1])
            seconds = float(parts[2])
            return hours * 3600 + minutes * 60 + seconds
    except Exception:
        pass
    return 0.0


def main():
    search_pattern = os.environ.get("TRX_PATTERN", "TestResults/**/*.trx")
    trx_files = glob.glob(search_pattern, recursive=True)

    summary_file = os.environ.get("GITHUB_STEP_SUMMARY")

    if not trx_files:
        msg = "### 🧪 .NET Test Results\n\nNo `.trx` test result files found.\n"
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

    suites = {}
    failed_tests = []

    for file_path in sorted(trx_files):
        suite_name = os.path.splitext(os.path.basename(file_path))[0]
        try:
            tree = ET.parse(file_path)
            root = tree.getroot()
        except Exception as e:
            print(f"Warning: Failed to parse {file_path}: {e}", file=sys.stderr)
            continue

        counters = None
        for elem in root.iter():
            if elem.tag.endswith("Counters"):
                counters = elem
                break

        s_total = int(counters.get("total", 0)) if counters is not None else 0
        s_passed = int(counters.get("passed", 0)) if counters is not None else 0
        s_failed = int(counters.get("failed", 0)) if counters is not None else 0
        s_skipped = int(counters.get("notExecuted", 0)) if counters is not None else 0

        total_tests += s_total
        total_passed += s_passed
        total_failed += s_failed
        total_skipped += s_skipped

        s_duration = 0.0

        for elem in root.iter():
            if elem.tag.endswith("UnitTestResult"):
                outcome = elem.get("outcome", "")
                test_name = elem.get("testName", "UnknownTest")
                dur_str = elem.get("duration", "")
                dur_sec = parse_duration_to_seconds(dur_str)
                s_duration += dur_sec

                if outcome not in ("Passed", "NotExecuted"):
                    msg = ""
                    stack = ""
                    for child in elem.iter():
                        if child.tag.endswith("Message") and child.text:
                            msg = child.text.strip()
                        elif child.tag.endswith("StackTrace") and child.text:
                            stack = child.text.strip()
                    failed_tests.append({
                        "suite": suite_name,
                        "test": test_name,
                        "outcome": outcome,
                        "message": msg,
                        "stack": stack,
                    })

        total_duration_sec += s_duration
        suites[suite_name] = {
            "total": s_total,
            "passed": s_passed,
            "failed": s_failed,
            "skipped": s_skipped,
            "duration": s_duration,
        }

    status_icon = "🟢" if total_failed == 0 else "🔴"
    status_label = "Passed" if total_failed == 0 else "Failed"

    lines = []
    lines.append(f"## {status_icon} .NET Test Results: {status_label}\n")
    lines.append("| Metric | Count |")
    lines.append("| :--- | :---: |")
    lines.append(f"| **Total Tests** | {total_tests} |")
    lines.append(f"| 🟢 **Passed** | {total_passed} |")
    lines.append(f"| 🔴 **Failed** | {total_failed} |")
    lines.append(f"| 🟡 **Skipped** | {total_skipped} |")
    lines.append(f"| ⏱️ **Duration** | {total_duration_sec:.2f}s |\n")

    if suites:
        lines.append("### 📦 Test Suites Breakdown\n")
        lines.append("| Suite / Project | Total | Passed | Failed | Skipped | Duration | Outcome |")
        lines.append("| :--- | :---: | :---: | :---: | :---: | :---: | :---: |")
        for name, data in sorted(suites.items()):
            icon = "✅" if data["failed"] == 0 else "❌"
            lines.append(
                f"| `{name}` | {data['total']} | {data['passed']} | "
                f"{data['failed']} | {data['skipped']} | {data['duration']:.2f}s | {icon} |"
            )
        lines.append("")

    if failed_tests:
        lines.append("### ❌ Failed Test Details\n")
        for f in failed_tests:
            lines.append(f"<details><summary><b>[{f['suite']}] {f['test']}</b> ({f['outcome']})</summary>\n")
            if f["message"]:
                lines.append(f"**Error Message:**\n```text\n{f['message']}\n```\n")
            if f["stack"]:
                lines.append(f"**Stack Trace:**\n```text\n{f['stack']}\n```\n")
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
