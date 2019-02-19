﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nuget.Updater.Entities;
using NuGet.Configuration;

namespace Nuget.Updater.Extensions
{
	internal static class ParametersExtension
	{
		internal static bool HasUpdateTarget(this NuGetUpdater.Parameters parameters, UpdateTarget target) => (parameters.UpdateTarget & target) == target;

		internal static PackageSource GetFeedPackageSource(this NuGetUpdater.Parameters parameters) => new PackageSource(parameters.SourceFeed, "Feed")
		{
			Credentials = PackageSourceCredential.FromUserInput("Feed", "user", parameters.SourceFeedPersonalAccessToken, false)
		};

		internal static bool ShouldUpdatePackage(this NuGetUpdater.Parameters parameters, NuGetPackage package) =>
			(parameters.PackagesToIgnore == null || !parameters.PackagesToIgnore.Contains(package.PackageId, StringComparer.OrdinalIgnoreCase))
			&& (parameters.PackagesToUpdate == null || parameters.PackagesToUpdate.Contains(package.PackageId, StringComparer.OrdinalIgnoreCase));

		internal static bool ShouldKeepPackageAtLatestDev(this NuGetUpdater.Parameters parameters, string packageId) =>
			parameters.PackagesToKeepAtLatestDev != null && parameters.PackagesToKeepAtLatestDev.Contains(packageId, StringComparer.OrdinalIgnoreCase);
	}
}
