using OculusSampleFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace CapitansLog.Scripts
{
	internal class CapitansLogGameObject : MonoBehaviour
	{

		public int currentDay = -1;
		private string path;

		private Vector2 lastPos;

		public void StartRecording()
		{
			lastPos = GetCurrentPlayerCoords();

			string logDirectory = Path.Combine(
				CapitansLogMain.instance.GetFolderLocation(),
				"coords_" + SaveSlots.currentSlot
			);
			if (!Directory.Exists(logDirectory))
			{
				Directory.CreateDirectory(logDirectory);
			}
			var logFiles = Directory.GetFiles(logDirectory, "log_*.txt");
			if (logFiles.Length >= CapitansLogMain.instance.log_saved.Value)
			{
				var orderedFiles = logFiles.OrderBy(File.GetCreationTime).ToArray();
				File.Delete(orderedFiles[0]);
			}

			path = Path.Combine(CapitansLogMain.instance.GetFolderLocation(), "coords_" + SaveSlots.currentSlot, "log_" + DateTime.Now.ToString(CapitansLogMain.instance.file_name_date_format.Value) + ".txt");
			StartCoroutine(LoopCorutine());
		}

		private Vector2 GetCurrentPlayerCoords()
		{
			if (FloatingOriginManager.instance is null) return new Vector2(0, 0);
			if (Refs.charController is null) return new Vector2(0, 0);
			try
			{
				var fom = FloatingOriginManager.instance;
				Transform pos = FloatingOriginManager.instance.shifterObject;
				var globeCoords = fom.GetGlobeCoords(pos);
				return new Vector2(globeCoords.x, globeCoords.z);
			}
			catch (TypeInitializationException ex)
			{
				CapitansLogMain.logSource.LogInfo($"GetCurrentPlayerCoords ex:{ex.InnerException}");
				return new Vector2(0, 0);
			}
			catch (Exception e)
			{
				CapitansLogMain.logSource.LogInfo($"GetCurrentPlayerCoords e:{e.Message}");
				return new Vector2(0, 0);
			}

		}

		public IEnumerator LoopCorutine()
		{
			yield return new WaitForEndOfFrame();
			while (GameState.currentlyLoading)
			{
				yield return null;
			}
			while (true)
			{
				RecordCurrentCoords();
				yield return new WaitForSeconds(CapitansLogMain.instance.recordTimer.Value);
			}
		}

		public void RecordCurrentCoords()
		{
			if (currentDay != GameState.day)
			{
				currentDay = GameState.day;
				var str2 = "Day: " + currentDay + Environment.NewLine;
				File.AppendAllText(path, str2);
			}
			var playerPos = GetCurrentPlayerCoords();
			float time = Sun.sun.globalTime;
			var currentWind = Wind.currentWind;
			var str = playerPos.y + " " + playerPos.x + " " + time + " " + NormalizeAngle(Mathf.Atan2(currentWind.z, currentWind.x) * 180f / Mathf.PI) + Environment.NewLine;
			File.AppendAllText(path, str);
			lastPos = playerPos;
		}

		private static float NormalizeAngle(float angle)
		{
			while (angle < 0)
			{
				angle += 360;
			}

			while (angle >= 360)
			{
				angle -= 360;
			}

			return angle;
		}
	}
}
