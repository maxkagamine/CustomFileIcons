using System.Collections.Generic;
using System.Diagnostics;

namespace CustomFileIcons
{
    [DebuggerDisplay("Extension = {Extension}, Name = {Name}")]
    public class FileType
    {
        /// <summary>
        /// File type description
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Primary extension
        /// </summary>
        public string Extension { get; set; }

        /// <summary>
        /// Any additional extensions associated with the type
        /// </summary>
        public List<string> Aliases { get; set; } = new List<string>();

        /// <summary>
        /// Icon name matching an "{icon}.*" file in the icons directory,
        /// or the full path to an icon; defaults to the extension
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// The open command, either the key of a defined command or the
        /// full command itself; defaults to "default"
        /// </summary>
        public string Open { get; set; }

        /// <summary>
        /// Additional menu items as a map of name to command; use an ampersand in
        /// the name to specify the underlined "access key" (double for a literal)
        /// </summary>
        public Dictionary<string, string> Menu { get; set; } = new Dictionary<string, string>();
    }
}
