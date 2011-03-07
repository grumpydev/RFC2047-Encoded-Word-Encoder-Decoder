//===============================================================================
// RFC2047 (Encoded Word) Decoder
//
// http://tools.ietf.org/html/rfc2047
//===============================================================================
// Copyright © Steven Robbins.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===============================================================================
﻿namespace EncodedWord
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Provides support for decoding RFC2047 (Encoded Word) encoded text
    /// </summary>
    public static class RFC2047
    {
        /// <summary>
        /// Regex for parsing encoded word sections
        /// From http://tools.ietf.org/html/rfc2047#section-3
        /// encoded-word = "=?" charset "?" encoding "?" encoded-text "?="
        /// </summary>
        private static readonly Regex EncodedWordFormatRegEx = new Regex(@"=\?(?<charset>.*?)\?(?<encoding>[qQbB])\?(?<encodedtext>.*?)\?=", RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// Regex for "Q-Encoding" hex bytes from http://tools.ietf.org/html/rfc2047#section-4.2
        /// </summary>
        private static readonly Regex QEncodingHexCodeRegEx = new Regex(@"(=(?<hexcode>[0-9a-fA-F][0-9a-fA-F]))", RegexOptions.Compiled);

        /// <summary>
        /// Regex for replacing _ with space as declared in http://tools.ietf.org/html/rfc2047#section-4.2
        /// </summary>
        private static readonly Regex QEncodingSpaceRegEx = new Regex("_", RegexOptions.Compiled);

        /// <summary>
        /// Represents a content encoding type defined in RFC2047
        /// </summary>
        private enum ContentEncoding
        {
            /// <summary>
            /// Unknown / invalid encoding
            /// </summary>
            Unknown,

            /// <summary>
            /// "Q Encoding" (reduced character set) encoding
            /// http://tools.ietf.org/html/rfc2047#section-4.2
            /// </summary>
            QEncoding,

            /// <summary>
            /// Base 64 encoding
            /// http://tools.ietf.org/html/rfc2047#section-4.1
            /// </summary>
            Base64
        }

        /// <summary>
        /// Decode a string containing RFC2047 encoded sections
        /// </summary>
        /// <param name="encodedString">String contaning encoded sections</param>
        /// <returns>Decoded string</returns>
        public static string Decode(string encodedString)
        {
            return EncodedWordFormatRegEx.Replace(
                encodedString,
                m =>
                {
                    var contentEncoding = GetContentEncodingType(m.Groups["encoding"].Value);
                    if (contentEncoding == ContentEncoding.Unknown)
                    {
                        // Regex should never match, but return anyway
                        return string.Empty;
                    }

                    var characterSet = m.Groups["charset"].Value;
                    if (!IsSupportedCharacterSet(characterSet))
                    {
                        // Fall back to iso-8859-1 if invalid/unsupported character set found
                        characterSet = @"iso-8859-1";
                    }

                    var textEncoding = Encoding.GetEncoding(characterSet);
                    var contentDecoder = GetContentDecoder(contentEncoding);
                    var encodedText = m.Groups["encodedtext"].Value;

                    return contentDecoder.Invoke(encodedText, textEncoding);
                });
        }

        /// <summary>
        /// Determines if a character set is supported
        /// </summary>
        /// <param name="characterSet">Character set name</param>
        /// <returns>Bool representing whether the character set is supported</returns>
        private static bool IsSupportedCharacterSet(string characterSet)
        {
            return Encoding.GetEncodings()
                           .Where(e => String.Equals(e.Name, characterSet, StringComparison.InvariantCultureIgnoreCase))
                           .Any();
        }

        /// <summary>
        /// Gets the content encoding type from the encoding character
        /// </summary>
        /// <param name="contentEncodingCharacter">Content contentEncodingCharacter character</param>
        /// <returns>ContentEncoding type</returns>
        private static ContentEncoding GetContentEncodingType(string contentEncodingCharacter)
        {
            switch (contentEncodingCharacter)
            {
                case "Q":
                case "q":
                    return ContentEncoding.QEncoding;
                case "B":
                case "b":
                    return ContentEncoding.Base64;
                default:
                    return ContentEncoding.Unknown;
            }
        }

        /// <summary>
        /// Gets the content decoder delegate for the given content encoding type
        /// </summary>
        /// <param name="contentEncoding">Content encoding type</param>
        /// <returns>Decoding delegate</returns>
        private static Func<string, Encoding, string> GetContentDecoder(ContentEncoding contentEncoding)
        {
            switch (contentEncoding)
            {
                case ContentEncoding.Base64:
                    return DecodeBase64;
                case ContentEncoding.QEncoding:
                    return DecodeQEncoding;
                default:
                    // Will never get here, but return a "null" delegate anyway
                    return (s, e) => String.Empty;
            }
        }

        /// <summary>
        /// Decodes a base64 encoded string
        /// </summary>
        /// <param name="encodedText">Encoded text</param>
        /// <param name="textEncoder">Encoding instance for the code page required</param>
        /// <returns>Decoded string</returns>
        private static string DecodeBase64(string encodedText, Encoding textEncoder)
        {
            var encodedBytes = Convert.FromBase64String(encodedText);

            return textEncoder.GetString(encodedBytes);
        }

        /// <summary>
        /// Decodes a "Q encoded" string
        /// </summary>
        /// <param name="encodedText">Encoded text</param>
        /// <param name="textEncoder">Encoding instance for the code page required</param>
        /// <returns>Decoded string</returns>
        private static string DecodeQEncoding(string encodedText, Encoding textEncoder)
        {
            var decodedText = QEncodingSpaceRegEx.Replace(encodedText, " ");
            
            decodedText = QEncodingHexCodeRegEx.Replace(
                decodedText,
                m =>
                {
                    var hexString = m.Groups["hexcode"].Value;

                    int characterValue;
                    if (!int.TryParse(hexString, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out characterValue))
                    {
                        return String.Empty;
                    }

                    return textEncoder.GetString(new[] { (byte)characterValue });
                });

            return decodedText;
        }
    }
}