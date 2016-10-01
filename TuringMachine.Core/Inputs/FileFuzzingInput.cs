﻿using System.IO;
using TuringMachine.Core.Interfaces;

namespace TuringMachine.Core.Inputs
{
    public class FileFuzzingInput : IFuzzingInput
    {
        /// <summary>
        /// Name
        /// </summary>
        public string FileName { get; private set; }
        /// <summary>
        /// Type
        /// </summary>
        public string Type { get { return "File"; } }
        /// <summary>
        /// IsSelectable
        /// </summary>
        public bool IsSelectable { get { return true; } }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="filename">File</param>
        public FileFuzzingInput(string filename)
        {
            FileName = filename;
        }
        /// <summary>
        /// Get file stream
        /// </summary>
        public Stream GetStream()
        {
            return File.OpenRead(FileName);
        }
        /// <summary>
        /// String representation
        /// </summary>
        public override string ToString()
        {
            return FileName;
        }
    }
}