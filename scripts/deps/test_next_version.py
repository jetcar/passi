import unittest

from next_version import next_version


class NextVersionTests(unittest.TestCase):
    def test_increments_minor_of_highest_tag(self):
        self.assertEqual(next_version(["android-v1.56", "android-v1.57"], base="1.0"), "1.58")

    def test_compares_numerically_not_lexically(self):
        self.assertEqual(next_version(["android-v1.9", "android-v1.10"], base="1.0"), "1.11")

    def test_ignores_unrelated_and_malformed_tags(self):
        self.assertEqual(next_version(["v2.0", "android-vfoo", "android-v1.57", "backend-1.99"], base="1.0"), "1.58")

    def test_falls_back_to_base_when_no_release_tags(self):
        self.assertEqual(next_version([], base="1.57"), "1.58")

    def test_base_wins_when_higher_than_tags(self):
        self.assertEqual(next_version(["android-v1.50"], base="1.57"), "1.58")


if __name__ == "__main__":
    unittest.main()
