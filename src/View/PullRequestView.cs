using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mono.Terminal;
using PrDash.DataSource;
using Terminal.Gui;

namespace PrDash.View
{
    /// <summary>
    /// Custom ListView implementation that to support custom key processing.
    /// </summary>
    /// <seealso cref="Terminal.Gui.ListView" />
    [SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "This isn't a collection")]
    [SuppressMessage("Design", "CA1010:Collections should implement generic interface", Justification = "No need to implement more enumerator")]
    public class PullRequestView : ListView
    {
        /// <summary>
        /// The interval in which we should refresh this views data.
        /// </summary>
        private static TimeSpan RefreshTimerInterval = TimeSpan.FromMinutes(30);

        /// <summary>
        /// The contents of the view when the data is loading.
        /// </summary>
        private static List<string> LoadingContents = new List<string>()
        {
            " Loading..."
        };

        /// <summary>
        /// The pull request source data source used to refresh the view's state.
        /// </summary>
        private readonly IPullRequestSource m_pullRequestSource;

        /// <summary>
        /// The list of elements that this view is currently rendering.
        /// </summary>
        private List<PullRequestViewElement> m_backingList = new List<PullRequestViewElement>();

        /// <summary>
        /// Monitor to synchronize the updating of the view.
        /// </summary>
        private readonly object m_viewRefreshMonitor = new object();

        /// <summary>
        /// The state we wish to view pull requests in.
        /// </summary>
        private PrState m_stateToView = PrState.Actionable;

        /// <summary>
        /// Initializes a new instance of the <see cref="PullRequestView"/> class.
        /// </summary>
        /// <param name="source">The source of elements for this view.</param>
        public PullRequestView(IPullRequestSource source)
            : base(LoadingContents)
        {
            m_pullRequestSource = source;

            // Override the color scheme to our main theme for this view.
            //
            ColorScheme = CustomColorSchemes.Main;

            // Post an async task to populate this view.
            //
            RefreshListDataAsync();

            // Setup a timer to fire in the future to refresh again.
            //
            Application.MainLoop.AddTimeout(RefreshTimerInterval, RefreshTimerCallback);
        }

        /// <summary>
        /// Switch what pull requests we want to view.
        /// </summary>
        /// <param name="newState">What state we want the pull requests we are loking at to be in.</param>
        private void SwitchPrStateView(PrState newState)
        {
            // TODO: Switch state should toggle the title of the view.
            //
            m_stateToView = newState;
            RefreshListDataAsync();
        }

        /// <summary>
        /// Implement custom key handling on this view.
        /// </summary>
        /// <param name="keyEvent">Contains the details about the key that produced the event.</param>
        /// <returns>True if the key was handled, False otherwise.</returns>
        public override bool ProcessKey(KeyEvent keyEvent)
        {
            // Handle specific characters we want to have behavior.
            //
            char keyChar = (char)((uint)keyEvent.Key & (uint)(Key.CharMask));
            switch (keyChar)
            {
                // Map vim Keys to be up and down.
                //
                case 'j':
                    keyEvent.Key = Key.CursorDown;
                    return base.ProcessKey(keyEvent);

                case 'k':
                    keyEvent.Key = Key.CursorUp;
                    return base.ProcessKey(keyEvent);

                // Enable hotkey refresh.
                //
                case 'r':
                    RefreshListDataAsync();
                    return true;

                // Enable hotkey switch view to waiting.
                //
                case 'w':
                    SwitchPrStateView(PrState.Waiting);
                    return true;

                // Enable hotkey switch view to actionable.
                //
                case 'a':
                    SwitchPrStateView(PrState.Actionable);
                    return true;

                // Enable hotkey help.
                //
                case 'h':
                    HelpDialog.Launch();
                    return true;

                // Enable hotkey quit.
                //
                case 'q':
                    Application.RequestStop();
                    return true;
            }

            // Handle special characters.
            //
            switch (keyEvent.Key)
            {
                // Hook Control-C and Esc as exit.
                //
                case Key.Esc:
                case Key.ControlC:
                    Application.RequestStop();
                    return true;

                // Hook Enter to open the given pull request under the cursor.
                //
                case Key.Enter:
                    m_backingList[SelectedItem].InvokeHandler();
                    return true;
            }

            // Forward everything else to the real implementation.
            //
            return base.ProcessKey(keyEvent);
        }

        /// <summary>
        /// Timer callback to refresh the view on the timer interval.
        /// </summary>
        /// <param name="MainLoop">The main loop.</param>
        /// <returns>True if the timer should be re-added, false otherwise.</returns>
        private bool RefreshTimerCallback(MainLoop main)
        {
            RefreshListDataAsync();
            return true;
        }

        /// <summary>
        /// Refreshes the list data asynchronous.
        /// </summary>
        private void RefreshListDataAsync()
        {
            bool needsRelease = false;
            try
            {
                // We will enter the monitor for the duration of the refresh.
                // See the exit at the bottom of RefreshListDataCallBack.
                //
                if (Monitor.TryEnter(m_viewRefreshMonitor))
                {
                    needsRelease = true;

                    // Clear the backing list, but don't re-render yet.
                    //
                    m_backingList.Clear();

                    Task task = new Task(() =>
                    {
                        try
                        {
                            // Invoke the callback, wait for the population to finish.
                            //
                            RefreshListDataCallBack().Wait();
                        }
                        finally
                        {
                            // Once population is finished, exit the monitor.
                            //
                            Application.MainLoop.Invoke(() =>
                            {
                                // Exit the monitor that was entered
                                // before the task was launched.
                                // See: RefreshListDataAsync
                                //
                                Monitor.Exit(m_viewRefreshMonitor);
                            });
                        }
                    });

                    task.Start();

                    needsRelease = false;
                }
            }
            finally
            {
                if (needsRelease)
                {
                    Monitor.Exit(m_viewRefreshMonitor);
                }
            }
        }

        /// <summary>
        /// Populate the list of pull requests from the data source.
        /// </summary>
        /// <returns>A list of pull request content.</returns>
        private async Task RefreshListDataCallBack()
        {
            try
            {
                await foreach (PullRequestViewElement element in m_pullRequestSource.FetchPullRequests(m_stateToView))
                {
                    // Force the re-rendering to occur on the main thread.
                    //
                    Application.MainLoop.Invoke(() =>
                    {
                        m_backingList.Add(element);

                        // Sort the entries by the elements sort implementation.
                        //
                        m_backingList.Sort();

                        SetSource(m_backingList);
                        SelectedItem = 0;
                        TopItem = 0;
                        SetNeedsDisplay();
                        Application.Refresh();
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Application.RequestStop();
                Environment.Exit(1);
                throw;
            }
        }
    }
}
