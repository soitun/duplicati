// Copyright (C) 2025, The Duplicati Team
// https://duplicati.com, hello@duplicati.com
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

#nullable enable

using System.Linq;
using System.Threading.Tasks;
using Duplicati.Library.Interface;

namespace Duplicati.Library.Main.Operation;

/// <summary>
/// Handler for listing folder contents
/// </summary>
internal static class ListFolderHandler
{
    /// <summary>
    /// Lists the contents of a folder in a backup fileset
    /// </summary>
    /// <param name="options">The options to use</param>
    /// <param name="result">The result class</param>
    /// <param name="folders">The folders to list</param>
    /// <param name="offset">The offset to start listing from</param>
    /// <param name="limit">The maximum number of entries to list</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public static async Task RunAsync(Options options, ListFolderResults result, string[] folders, long offset, long limit)
    {
        if (!System.IO.File.Exists(options.Dbpath) || options.NoLocalDb)
            throw new UserInformationException("No local database found, this operation requires a local database", "NoLocalDatabase");

        await using var db = await Database.LocalListDatabase.CreateAsync(options.Dbpath, null, result.TaskControl.ProgressToken)
            .ConfigureAwait(false);

        var filesetIds = await db
            .GetFilesetIDs(options.Time, options.Version, singleTimeMatch: true, result.TaskControl.ProgressToken)
            .ToArrayAsync(cancellationToken: result.TaskControl.ProgressToken)
            .ConfigureAwait(false);

        if (filesetIds.Length == 0)
            throw new UserInformationException("No filesets found", "NoFilesetsFound");
        if (filesetIds.Length > 1)
            throw new UserInformationException("Multiple filesets found, please specify a single version or time", "MultipleFilesetsFound");

        if (folders == null || folders.Length == 0 || folders[0] == "")
        {
            if (folders != null && folders.Length > 1)
                throw new UserInformationException("When no folder is specified, only one folder can be listed", "MultipleFoldersFound");
            var rootFolders = await db
                .GetMinimalUniquePrefixEntries(filesetIds[0], result.TaskControl.ProgressToken)
                .ToListAsync(cancellationToken: result.TaskControl.ProgressToken)
                .ConfigureAwait(false);
            result.Entries = new PaginatedResults<IListFolderEntry>(0, rootFolders.Count, 1, rootFolders.Count, rootFolders);
        }
        else
        {
            result.Entries = await db
                .ListFolder(
                    db.GetPrefixIds(folders, result.TaskControl.ProgressToken).ToEnumerable(),
                    filesetIds[0],
                    offset,
                    limit,
                    result.TaskControl.ProgressToken
                )
                .ConfigureAwait(false);
        }
    }
}
