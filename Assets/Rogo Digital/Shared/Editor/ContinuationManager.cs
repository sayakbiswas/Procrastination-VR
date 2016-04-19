using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace RogoDigital {
	internal static class ContinuationManager
	{
		public class Job
		{
			public Job(Func<bool> completed, Action continueWith)
			{
				Completed = completed;
				ContinueWith = continueWith;
			}
			public Func<bool> Completed { get; private set; }
			public Action ContinueWith { get; private set; }
		}
		
		private static readonly List<Job> jobs = new List<Job>();
		
		public static Job Add(Func<bool> completed, Action continueWith)
		{
			if (!jobs.Any()) EditorApplication.update += Update;
			Job job = new Job(completed, continueWith);
			jobs.Add(job);
			return job;
		}

		public static void Cancel(Job job) {
			jobs.Remove(job);
		}

		private static void Update()
		{
			for (int i = 0; i >= 0; --i)
			{
				var jobIt = jobs[i];
				if (jobIt.Completed())
				{
					jobIt.ContinueWith();
					jobs.RemoveAt(i);
				}
			}
			if (!jobs.Any()) EditorApplication.update -= Update;
		}
	}
}