// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace SideCar.Extensions
{
	internal static class HashExtensions
	{
		// FIXME: vary on content in the assembly!
		public static string ComputePackageHash(this Assembly assembly, string buildHash)
		{
			var assemblyHash = assembly.GetName().Name;
			var packageHash = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes($"{assemblyHash}_{buildHash}"));
			return BitConverter.ToString(packageHash).Replace("-", "");
		}
	}
}
