using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CapitansLog.Scripts;
using HarmonyLib;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace CapitansLog
{
	[BepInPlugin(GUID, NAME, VERSION)]
	public class CapitansLogMain : BaseUnityPlugin
	{
		public const string GUID = "com.gesterom.captains_log";
		public const string NAME = "Captain's' Log";
		public const string VERSION = "1.0.0";

		public static ManualLogSource logSource;

		public sealed class SaveSlotEventArgs
		{
			public int SaveSlot { get; }

			public SaveSlotEventArgs(int saveSlot)
			{
				SaveSlot = saveSlot;
			}
		}
		public delegate void SaveSlotEventHandler(object sender, SaveSlotEventArgs e);
		public static event SaveSlotEventHandler OnSaveLoad;
		public static event SaveSlotEventHandler OnSaveLoadPost;
		public static event SaveSlotEventHandler OnNewGame;

		internal ConfigEntry<float> recordTimer;
		internal ConfigEntry<int> log_saved;
		internal ConfigEntry<string> file_name_date_format;

		internal static CapitansLogMain instance;

		private void Awake()
		{
			instance = this;
			logSource = Logger;
			Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), GUID);
			recordTimer = Config.Bind("Values", "recordTimer", 30f, new ConfigDescription("Every how many seconds is the coordinates recorded?"));
			log_saved = Config.Bind("Values", "log_saved", 5, new ConfigDescription("How many recent records are stored?"));
			file_name_date_format = Config.Bind("Values", "file_name_date_format", "yyyy_MM_dd_HH_mm", new ConfigDescription("Date format used as part of file name for record."));

			OnNewGame += (_, __) =>
			{
				StartCoroutine(CreateCapitanLogGameObject(-1));
			};

			OnSaveLoad += (_, __) =>
			{
				StartCoroutine(CreateCapitanLogGameObject(GameState.day));
			};
		}

		private static IEnumerator CreateCapitanLogGameObject(int p_day)
		{
			yield return new WaitForEndOfFrame();
			while (GameState.currentlyLoading)
			{
				yield return null;
			}
			GameObject ui = new GameObject();
			var component = ui.AddComponent<CapitansLogGameObject>();
			component.currentDay = p_day;
			component.StartRecording();
		}

		[HarmonyPatch(typeof(StartMenu), "StartNewGame")]
		private static class SaveLoadNew
		{
			[HarmonyPostfix]
			public static void Postfix(StartMenu __instance)
			{
				OnNewGame?.Invoke(__instance, new SaveSlotEventArgs(SaveSlots.currentSlot));
			}
		}

		[HarmonyPatch(typeof(SaveLoadManager), "LoadGame")]
		private static class SaveLoad
		{
			[HarmonyPostfix]
			public static void Postfix(StartMenu __instance)
			{
				OnSaveLoad?.Invoke(__instance, new SaveSlotEventArgs(SaveSlots.currentSlot));
				OnSaveLoadPost?.Invoke(__instance, new SaveSlotEventArgs(SaveSlots.currentSlot));
			}
		}

		public string GetFolderLocation()
		{
			return Directory.GetParent(this.Info.Location).FullName;
		}
	}
}
