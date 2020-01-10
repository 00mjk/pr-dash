﻿using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using PrDash.View;

namespace PrDash.DataSource
{
    /// <summary>
    /// Event arguments for callback on statistics updates.
    /// </summary>
    public class StatisticsUpdateEventArgs
    {
        public PullRequestStatistics Statistics { set; get; }
    }

    /// <summary>
    /// Interface for interacting with the pull request.
    /// </summary>
    public interface IPullRequestSource
    {
        /// <summary>
        /// Event to list on statistics updates.
        /// </summary>
        event EventHandler<StatisticsUpdateEventArgs>  StatisticsUpdate;

        /// <summary>
        /// Retrieves all active pull requests to the configured data source.
        /// </summary>
        /// <returns>A stream of <see cref="GitPullRequest"/></returns>
        IAsyncEnumerable<PullRequestViewElement> FetchActivePullRequsts();
    }
}
