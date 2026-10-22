import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

CHECKER = Path(__file__).with_name('check_script.py')

class QuoteChecks(unittest.TestCase):
    def check_row(self, text, source):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'script.md'
            path.write_text('Language: he\n| # | Time | Picture | On-screen text | Voice-over | Sources |\n|---|---|---|---|---|---|\n| 1 | 0-5 | Card | ' + text + ' | | ' + source + ' |\n', encoding='utf-8')
            return subprocess.run([sys.executable, str(CHECKER), str(path)], capture_output=True, text=True, encoding='utf-8')

    def test_hebrew_acronym_needs_fact_not_quote_id(self):
        result = self.check_row('רמטכ״ל ההגנה', 'F1')
        self.assertEqual(result.returncode, 0, result.stdout)

    def test_actual_quote_without_quote_id_fails(self):
        result = self.check_row('"מילים לבדיקה"', 'F1')
        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn('quotation with no Q id', result.stdout)

    def test_actual_quote_with_quote_id_passes(self):
        result = self.check_row('"מילים לבדיקה"', 'Q1')
        self.assertEqual(result.returncode, 0, result.stdout)

if __name__ == '__main__':
    unittest.main()
