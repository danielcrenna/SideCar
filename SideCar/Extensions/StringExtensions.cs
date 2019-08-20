// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace SideCar.Extensions
{
	public static class StringExtensions
	{
		public static string ToCamelCase(this string s)
		{
			if (string.IsNullOrEmpty(s) || !char.IsUpper(s[0]))
				return s;
			var buffer = s.ToCharArray();
			for (var index = 0; index < buffer.Length && (index != 1 || char.IsUpper(buffer[index])); ++index)
			{
				if (index > 0 & index + 1 < buffer.Length && !char.IsUpper(buffer[index + 1]))
				{
					if (char.IsSeparator(buffer[index + 1]))
					{
						buffer[index] = char.ToLower(buffer[index]);
					}
					break;
				}
				buffer[index] = char.ToLower(buffer[index]);
			}
			return new string(buffer);
		}
	}
}
