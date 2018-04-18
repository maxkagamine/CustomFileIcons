/*
 * Copyright (c) 2003-2014 by
 *   Peter Astrand <astrand@lysator.liu.se> (python's subprocess module)
 *   Tim Cuthbertson <tim@gfxmonk.net> (C# port of this code)
 *
 * ======================= The MIT License ==============================
 *
 * Permission to use, copy, modify, and distribute this software and
 * its associated documentation for any purpose and without fee is
 * hereby granted, provided that the above copyright notice appears in
 * all copies, and that both that copyright notice and this permission
 * notice appear in supporting documentation, and that the name of the
 * author not be used in advertising or publicity pertaining to
 * distribution of the software without specific, written prior
 * permission.
 *
 * THE AUTHOR DISCLAIMS ALL WARRANTIES WITH REGARD TO THIS SOFTWARE,
 * INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS.
 * IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, INDIRECT OR
 * CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS
 * OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT,
 * NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION
 * WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
 */

// http://gfxmonk.net/2014/04/25/escaping-an-array-of-command-line-arguments-in-csharp.html

using System;
using System.Text;
using System.Collections.Generic;

namespace CustomFileIcons.Proxy
{
    public static class QuoteArguments
    {
        public static string Quote(IEnumerable<string> args)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string arg in args)
            {
                int backslashes = 0;

                // Add a space to separate this argument from the others
                if (sb.Length > 0)
                {
                    sb.Append(" ");
                }

                bool needquote = arg.Length == 0 || arg.Contains(" ") || arg.Contains("\t");
                if (needquote)
                {
                    sb.Append('"');
                }

                foreach (char c in arg)
                {
                    if (c == '\\')
                    {
                        // Don't know if we need to double yet.
                        backslashes++;
                    }
                    else if (c == '"')
                    {
                        // Double backslashes.
                        sb.Append(new String('\\', backslashes * 2));
                        backslashes = 0;
                        sb.Append("\\\"");
                    }
                    else
                    {
                        // Normal char
                        if (backslashes > 0)
                        {
                            sb.Append(new String('\\', backslashes));
                            backslashes = 0;
                        }
                        sb.Append(c);
                    }
                }

                // Add remaining backslashes, if any.
                if (backslashes > 0)
                {
                    sb.Append(new String('\\', backslashes));
                }

                if (needquote)
                {
                    sb.Append(new String('\\', backslashes));
                    sb.Append('"');
                }
            }
            return sb.ToString();
        }
    }
}