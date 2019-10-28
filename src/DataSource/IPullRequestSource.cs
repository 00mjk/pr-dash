﻿using System.Collections.Generic;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using PrDash.View;

namespace PrDash.DataSource
{
    /// <summary>
    /// Interface for interacting with the pull request.
    /// </summary>
    public interface IPullRequestSource
    {
        /// <summary>
        /// Getter to retreive statistics.
        /// </summary>
        PullRequestStatistics Statistics { get; }

        /// <summary>
        /// Retrieves all active pull requests to the configured data source.
        /// </summary>
        /// <returns>A stream of <see cref="GitPullRequest"/></returns>
        IAsyncEnumerable<PullRequestViewElement> FetchActivePullRequsts();
    }
}
