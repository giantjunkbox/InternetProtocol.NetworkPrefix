/*
   Bitvantage.IPNetworkExtensions
   Copyright (C) 2024 Michael Crino
   
   This program is free software: you can redistribute it and/or modify
   it under the terms of the GNU Affero General Public License as published by
   the Free Software Foundation, either version 3 of the License, or
   (at your option) any later version.
   
   This program is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU Affero General Public License for more details.
   
   You should have received a copy of the GNU Affero General Public License
   along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using System.Buffers;
using System.Text;

namespace Bitvantage.InternetProtocol;

/// <summary>
///     Parses UTF-8 text with a parser that reads characters, by converting the text to characters first.
/// </summary>
internal static class Utf8Text
{
    /// <summary>
    ///     The longest UTF-8 text that is converted to characters on the stack rather than in a rented array. Every valid address
    ///     or network is far shorter, but text may be padded with whitespace, so longer text is still parsed.
    /// </summary>
    private const int StackLimit = 256;

    /// <summary>
    ///     Parses text that has been converted to characters.
    /// </summary>
    /// <typeparam name="TResult">The type of the parsed value.</typeparam>
    /// <param name="text">The text.</param>
    /// <param name="result">When this method returns <see langword="true" />, the parsed value.</param>
    /// <returns><see langword="true" /> when the text is valid.</returns>
    internal delegate bool CharacterParser<TResult>(ReadOnlySpan<char> text, out TResult result);

    /// <summary>
    ///     Converts UTF-8 text to characters and parses them.
    /// </summary>
    /// <typeparam name="TResult">The type of the parsed value.</typeparam>
    /// <param name="utf8Text">The UTF-8 text.</param>
    /// <param name="parser">The parser for the characters.</param>
    /// <param name="result">When this method returns <see langword="true" />, the parsed value.</param>
    /// <returns><see langword="true" /> when the text is valid; invalid UTF-8 is never valid.</returns>
    internal static bool TryParse<TResult>(ReadOnlySpan<byte> utf8Text, CharacterParser<TResult> parser, out TResult result)
    {
        // UTF-8 never needs more characters than it has bytes, so a buffer as long as the text always fits it. Short text, which
        // is all valid text, converts on the stack
        char[]? rentedCharacters = null;
        var characters = utf8Text.Length <= StackLimit
            ? stackalloc char[StackLimit]
            : rentedCharacters = ArrayPool<char>.Shared.Rent(utf8Text.Length);

        try
        {
            // invalid UTF-8 becomes replacement characters, which then fail to parse
            var characterCount = Encoding.UTF8.GetChars(utf8Text, characters);
            return parser(characters[..characterCount], out result);
        }
        finally
        {
            if (rentedCharacters is not null)
                ArrayPool<char>.Shared.Return(rentedCharacters);
        }
    }
}
