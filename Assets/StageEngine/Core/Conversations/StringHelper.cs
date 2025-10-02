using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace StageEngine.Core.Conversations
{
    public static class StringHelper
    {
        /// <summary>
        /// Removes tags in the format [TAG] or [TAG WITH SPACES] and cleans up trailing spaces and newlines
        /// </summary>
        /// <param name="input">The input string containing tags</param>
        /// <returns>Cleaned string with tags removed and whitespace trimmed</returns>
        public static string RemoveTagsAndCleanup(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Remove tags in the format [TAG] or [TAG WITH SPACES]
            string result = Regex.Replace(input, @"\[.*?\]", string.Empty);

            // Remove empty lines and trim whitespace from each line
            var lines = result.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            var cleanedLines = new List<string>();

            foreach (var line in lines)
            {
                string trimmedLine = line.Trim();
                if (!string.IsNullOrEmpty(trimmedLine))
                {
                    cleanedLines.Add(trimmedLine);
                }
            }

            // Join back with single newlines and trim the final result
            return string.Join(Environment.NewLine, cleanedLines).Trim();
        }

        /// <summary>
        /// Alternative version that preserves paragraph structure (keeps single blank lines)
        /// </summary>
        /// <param name="input">The input string containing tags</param>
        /// <returns>Cleaned string with tags removed and minimal whitespace cleanup</returns>
        public static string RemoveTagsAndCleanupPreserveParagraphs(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var result = new StringBuilder();
            int bracketDepth = 0;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (c == '[')
                {
                    bracketDepth++;
                }
                else if (c == ']')
                {
                    bracketDepth--;
                }
                else if (bracketDepth == 0)
                {
                    // Only add characters when we're not inside any brackets
                    result.Append(c);
                }
            }

            // Clean up whitespace as before
            var lines = result.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            var cleanedLines = new List<string>();
            bool lastLineWasEmpty = false;

            foreach (var line in lines)
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine))
                {
                    if (!lastLineWasEmpty)
                    {
                        cleanedLines.Add(string.Empty);
                        lastLineWasEmpty = true;
                    }
                }
                else
                {
                    cleanedLines.Add(trimmedLine);
                    lastLineWasEmpty = false;
                }
            }

            return string.Join(Environment.NewLine, cleanedLines).Trim();
        }
    }
}