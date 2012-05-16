using System;
using System.Collections.Generic;
using System.Text;

namespace Ionic.Utils.Zip
{
    #region EventArgs classes

    /// <summary>
    /// Base class used to provide information about the download progress.
    /// </summary>
    public class SaveProgressEventArgs : EventArgs
    {

        private int _entriesTotal;
        private int _entriesSaved;
        private bool _cancel = false;
        private String _nameOfLatestEntry;

        /// <summary>
        /// Constructor for the SaveProgressEventArgs.
        /// </summary>
        /// <param name="entriesTotal">The total number of entries in the zip archive.</param>
        /// <param name="entriesSaved">Number of bytes that have been transferred.</param>
        /// <param name="lastEntry">The last entry saved.</param>
        internal SaveProgressEventArgs(int entriesTotal, int entriesSaved, string lastEntry)
        {
            this._entriesTotal = entriesTotal;
            this._entriesSaved = entriesSaved;
	    this._nameOfLatestEntry = lastEntry;
        }

        /// <summary>
        /// The total number of entries to be saved.
        /// </summary>
        public int EntriesTotal
        {
            get { return _entriesTotal; }
        }

        /// <summary>
        /// Number of entries saved so far.
        /// </summary>
        public int EntriesSaved
        {
            get { return _entriesSaved; }
        }


        /// <summary>
        /// the name of the last entry saved.
        /// </summary>
        public string NameOfLatestEntry
        {
            get { return _nameOfLatestEntry; }
        }

        /// <summary>
        /// Indicates whether the operation was cancelled or not.
        /// </summary>
        public bool Cancel
        {
            get { return _cancel; }
            set { _cancel = _cancel || value; }
        }
    }


    /// <summary>
    /// Used to provide event information about the Save .
    /// </summary>
    public class SaveEventArgs : EventArgs
    {
        private String _name;

	/// <summary>
        /// Constructor for a SaveEventArgs.
        /// </summary>
        /// <param name="archiveName">The name of the archive being saved.</param>
        internal SaveEventArgs(string archiveName)
        {
	  _name = archiveName;
        }

        /// <summary>
        /// Returns the archive name.
        /// </summary>
        public String ArchiveName
        {
            get { return _name; }
        }
    }


#endregion


    #region Save Events

    /// <summary>
    /// Delegate for the SaveProgress event.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The information about the event.</param>
    public delegate void SaveProgressEventHandler(object sender, SaveProgressEventArgs e);

    /// <summary>
    /// Delegate for the SaveStarted event.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The information about the event.</param>
    public delegate void SaveStartedEventHandler(object sender, SaveEventArgs e);

    /// <summary>
    /// Delegate for the SaveCompleted event.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The information about the event.</param>
    public delegate void SaveCompletedEventHandler(object sender, SaveEventArgs e);

    #endregion

}