﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Collections.Specialized;


namespace UXF
{
    /// <summary>
    /// The base unit of experiments. A Trial is usually a singular attempt at a task by a participant after/during the presentation of a stimulus.
    /// </summary>
    [Serializable]
    public class Trial : ISettingsContainer, IDataAssociatable
    {

        /// <summary>
        /// Returns non-zero indexed trial number. This is generated based on its position in the block, and the ordering of the blocks within the session.
        /// </summary>
        public int number { get { return session.Trials.ToList().IndexOf(this) + 1; } }

        /// <summary>
        /// Returns non-zero indexed trial number for the current block.
        /// </summary>
        public int numberInBlock { get { return block.trials.IndexOf(this) + 1; } }

        /// <summary>
        /// Status of the trial (enum)
        /// </summary>
        public TrialStatus status = TrialStatus.NotDone;

        /// <summary>
        /// The block associated with this session
        /// </summary>
        public Block block;
        float startTime, endTime;

        /// <summary>
        /// The session associated with this trial
        /// </summary>
        /// <returns></returns>
        public Session session { get; private set; }
        
        /// <summary>
        /// Trial settings. These will override block settings if set.
        /// </summary>
        public Settings settings { get; private set; }

        /// <summary>
        /// Dictionary of results in a order.
        /// </summary>
        public ResultsDictionary result;

        /// <summary>
        /// Manually create a trial. When doing this you need to add this trial to a block with block.trials.Add(trial)
        /// </summary>
        internal Trial(Block trialBlock)
        {
            settings = Settings.empty;
            SetReferences(trialBlock);
        }

        /// <summary>
        /// Set references for the trial.
        /// </summary>
        /// <param name="trialBlock">The block the trial belongs to.</param>
        private void SetReferences(Block trialBlock)
        {
            block = trialBlock;
            session = block.session;
            settings.SetParent(block);
        }

        /// <summary>
        /// Begins the trial, updating the current trial and block number, setting the status to in progress, starting the timer for the trial, and beginning recording positions of every object with an attached tracker
        /// </summary>
        public void Begin()
        {
            session.currentTrialNum = number;
            session.currentBlockNum = block.number;

            status = TrialStatus.InProgress;
            startTime = Time.time;
            result = new ResultsDictionary(session.Headers, true);

            result["experiment"] = session.experimentName;
            result["ppid"] = session.ppid;
            result["session_num"] = session.number;
            result["trial_num"] = number;
            result["block_num"] = block.number;
            result["trial_num_in_block"] = numberInBlock;
            result["start_time"] = startTime;

            foreach (Tracker tracker in session.trackedObjects)
            {
                tracker.StartRecording();
            }
            session.onTrialBegin.Invoke(this);
        }

        /// <summary>
        /// Ends the Trial, queues up saving results to output file, stops and saves tracked object data.
        /// </summary>
        public void End()
        {
            status = TrialStatus.Done;
            endTime = Time.time;
            result["end_time"] = endTime;            

            // log tracked objects
            foreach (Tracker tracker in session.trackedObjects)
            {
                SaveDataTable(tracker.data, tracker.dataName, dataType: DataType.Trackers);
            }

            // log any settings we need to for this trial
            foreach (string s in session.settingsToLog)
            {
                result[s] = settings.GetObject(s);
            }
            session.onTrialEnd.Invoke(this);
        }

        /// <summary>
        /// Saves a DataTable to the storage locations(s) for this trial. A column will be added in the trial_results CSV listing the location(s) of these data.
        /// </summary>
        /// <param name="table">The data to be saved.</param>
        /// <param name="dataName">Name to be used in saving. It will be appended with the trial number.</param>
        /// <param name="dataType"></param>
        public void SaveDataTable(UXFDataTable table, string dataName, DataType dataType = DataType.SessionInfo)
        {
            int i = 0;
            foreach(var dataHandler in session.ActiveDataHandlers)
            {
                string location = dataHandler.HandleDataTable(table, session.experimentName, session.ppid, session.number, string.Format("{0}_T{1:000}", dataName, number), dataType: dataType);
                result[string.Format("{0}_location_{1}", dataName, i++)] = location.Replace("\\", "/");
            }
        }

        /// <summary>
        /// Saves a JSON Serializable Object to the storage locations(s) for this trial. A column will be added in the trial_results CSV listing the location(s) of these data.
        /// </summary>
        /// <param name="serializableObject">The data to be saved.</param>
        /// <param name="dataName">Name to be used in saving. It will be appended with the trial number.</param>
        /// <param name="dataType"></param>
        public void SaveJSONSerializableObject(List<object> serializableObject, string dataName, DataType dataType = DataType.SessionInfo)
        {
            int i = 0;
            foreach(var dataHandler in session.ActiveDataHandlers)
            {
                string location = dataHandler.HandleJSONSerializableObject(serializableObject, session.experimentName, session.ppid, session.number, string.Format("{0}_T{1:000}", dataName, number), dataType: dataType);
                result[string.Format("{0}_location_{1}", dataName, i++)] = location.Replace("\\", "/");
            }
        }

        /// <summary>
        /// Saves a JSON Serializable Object to the storage locations(s) for this trial. A column will be added in the trial_results CSV listing the location(s) of these data.
        /// </summary>
        /// <param name="serializableObject">The data to be saved.</param>
        /// <param name="dataName">Name to be used in saving. It will be appended with the trial number.</param>
        /// <param name="dataType"></param>
        public void SaveJSONSerializableObject(Dictionary<string, object> serializableObject, string dataName, DataType dataType = DataType.SessionInfo)
        {
            int i = 0;
            foreach(var dataHandler in session.ActiveDataHandlers)
            {
                string location = dataHandler.HandleJSONSerializableObject(serializableObject, session.experimentName, session.ppid, session.number, string.Format("{0}_T{1:000}", dataName, number), dataType: dataType);
                result[string.Format("{0}_location_{1}", dataName, i++)] = location.Replace("\\", "/");
            }
        }

        /// <summary>
        /// Saves a string of text to the storage locations(s) for this trial. A column will be added in the trial_results CSV listing the location(s) of these data.
        /// </summary>
        /// <param name="text">The data to be saved.</param>
        /// <param name="dataName">Name to be used in saving. It will be appended with the trial number.</param>
        /// <param name="dataType"></param>
        public void SaveText(string text, string dataName, DataType dataType = DataType.SessionInfo)
        {
            int i = 0;
            foreach(var dataHandler in session.ActiveDataHandlers)
            {
                string location = dataHandler.HandleText(text, session.experimentName, session.ppid, session.number, string.Format("{0}_T{1:000}", dataName, number), dataType: dataType);
                result[string.Format("{0}_location_{1}", dataName, i++)] = location.Replace("\\", "/");
            }
        }

        /// <summary>
        /// Saves an array of bytes to the storage locations(s) for this trial. A column will be added in the trial_results CSV listing the location(s) of these data.
        /// </summary>
        /// <param name="bytes">The data to be saved.</param>
        /// <param name="dataName">Name to be used in saving. It will be appended with the trial number.</param>
        /// <param name="dataType"></param>
        public void SaveBytes(byte[] bytes, string dataName, DataType dataType = DataType.SessionInfo)
        {
            int i = 0;
            foreach(var dataHandler in session.ActiveDataHandlers)
            {
                string location = dataHandler.HandleBytes(bytes, session.experimentName, session.ppid, session.number, string.Format("{0}_T{1:000}", dataName, number), dataType: dataType);
                result[string.Format("{0}_location_{1}", dataName, i++)] = location.Replace("\\", "/");
            }
        }


    }

    

    /// <summary>
    /// Status of a trial
    /// </summary>
    public enum TrialStatus
    {
        NotDone,
        InProgress,
        Done
    }


}