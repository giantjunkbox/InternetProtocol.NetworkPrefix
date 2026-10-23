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

using System.ComponentModel;
using System.Globalization;
using System.Net;

namespace Bitvantage.InternetProtocol.Converters;

/// <summary>
///     Converts an <see cref="IPAddress" /> to and from text, so that configuration binding, and anything else that converts
///     through <see cref="TypeDescriptor" />, can read addresses from strings such as <c>"10.0.0.1"</c>.
/// </summary>
/// <remarks>
///     <para>
///         .NET has no type converter for <see cref="IPAddress" />, and without one, <c>Microsoft.Extensions.Configuration</c>
///         binding reports no error: it leaves an address property <see langword="null" />, and a list of addresses empty. Call
///         <see cref="Register" /> once at startup, before any configuration is bound. See <see cref="IPNetworkTypeConverter" />
///         for an example.
///     </para>
///     <para>
///         Text is read with <c>IPAddress.ParseStrict</c>, so invalid text, or text in a nonstandard IPv4 form, throws, and the
///         binder reports the configuration path of the bad value. The binder skips an element of a list that it can't
///         convert, though, without an error; when a bad entry in a list must not be dropped, bind the list as strings and parse
///         each with <c>IPAddress.ParseStrict</c>. The configuration binding source generator does not use type converters; with
///         it, bind a <see langword="string" /> and parse it the same way.
///     </para>
/// </remarks>
public sealed class IPAddressTypeConverter : TypeConverter
{
    /// <summary>
    ///     Whether <see cref="Register" /> has registered the converter; 1 when it has.
    /// </summary>
    private static int isRegistered;

    /// <summary>
    ///     Registers this converter as the type converter of <see cref="IPAddress" />, for the whole process. Calling it more than
    ///     once has no further effect.
    /// </summary>
    public static void Register()
    {
        // a second registration would add a second, identical provider in front of the first
        if (Interlocked.Exchange(ref isRegistered, 1) == 0)
            TypeDescriptor.AddAttributes(typeof(IPAddress), new TypeConverterAttribute(typeof(IPAddressTypeConverter)));
    }

    /// <summary>
    ///     Determines whether this converter can convert from a type: a <see langword="string" /> can be converted.
    /// </summary>
    /// <param name="context">The context of the conversion.</param>
    /// <param name="sourceType">The type to convert from.</param>
    /// <returns><see langword="true" /> when <paramref name="sourceType" /> is <see langword="string" />.</returns>
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    /// <summary>
    ///     Determines whether this converter can convert to a type: an address can be converted to a <see langword="string" />.
    /// </summary>
    /// <param name="context">The context of the conversion.</param>
    /// <param name="destinationType">The type to convert to.</param>
    /// <returns><see langword="true" /> when <paramref name="destinationType" /> is <see langword="string" />.</returns>
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }

    /// <summary>
    ///     Parses text as an address, with <c>IPAddress.ParseStrict</c>.
    /// </summary>
    /// <param name="context">The context of the conversion.</param>
    /// <param name="culture">The culture, which does not affect how an address is read.</param>
    /// <param name="value">The text to parse.</param>
    /// <returns>The address.</returns>
    /// <exception cref="FormatException">Thrown when the text is not a valid address.</exception>
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        return value is string addressText ? IPAddress.ParseStrict(addressText) : base.ConvertFrom(context, culture, value);
    }

    /// <summary>
    ///     Writes an address as text, such as <c>10.0.0.1</c> or <c>2001:db8::1</c>.
    /// </summary>
    /// <param name="context">The context of the conversion.</param>
    /// <param name="culture">The culture, which does not affect how an address is written.</param>
    /// <param name="value">The address.</param>
    /// <param name="destinationType">The type to convert to.</param>
    /// <returns>The text of the address.</returns>
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        return destinationType == typeof(string) && value is IPAddress address
            ? address.ToString()
            : base.ConvertTo(context, culture, value, destinationType);
    }

    /// <summary>
    ///     Determines whether a value can be converted to an address: text must be valid for <c>IPAddress.ParseStrict</c>.
    /// </summary>
    /// <param name="context">The context of the conversion.</param>
    /// <param name="value">The value to check.</param>
    /// <returns><see langword="true" /> when the value is an address, or text of one.</returns>
    public override bool IsValid(ITypeDescriptorContext? context, object? value)
    {
        return value switch
        {
            string addressText => IPAddress.TryParseStrict(addressText, out _),
            IPAddress => true,
            _ => false
        };
    }
}
