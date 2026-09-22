using System.Collections.Generic;
using System.Linq;
using LocalSave;
using UnityEngine;

namespace ABTesting
{
	public static class ABHelper
	{
		public static void EvaluateEligibleABs()
		{
			foreach (ABDefinition ab in ABLibrary.ABDefs)
			{
				ResolveAB(ab);
			}
		}

		public static string GetAbValueForKey(string controlledKey)
		{
			foreach (ABDefinition ab in ABLibrary.GetABsForKey(controlledKey))
			{
				ABVariantDefinition variant = GetPersistedVariant(ab);
				string value = variant?.GetValue(controlledKey);
				if (!string.IsNullOrWhiteSpace(value))
				{
					return value;
				}
			}
			return null;
		}

		private static void ResolveAB(ABDefinition ab)
		{
			PersistedABEntry entry = FindEntry(ab.Name);
			if (entry == null)
			{
				EnrollIfEligible(ab);
			}
			else
			{
				PromoteHoldoutIfEligible(ab, entry);
			}
		}

		private static PersistedABEntry FindEntry(string abName)
		{
			return SaveService.Data?.ABEntries?.data?.FirstOrDefault((PersistedABEntry x) => x.key == abName);
		}

		private static ABVariantDefinition GetPersistedVariant(ABDefinition ab)
		{
			PersistedABEntry entry = FindEntry(ab.Name);
			return entry?.variantId.HasValue == true ? FindVariantById(ab, entry.variantId.Value) : null;
		}

		private static void EnrollIfEligible(ABDefinition ab)
		{
			if (ab == null || !ab.AreConditionsMet())
			{
				return;
			}
			float participationDraw = Random.value;
			PersistedABEntry entry = new PersistedABEntry(ab.Name, participationDraw);
			PersistEntry(entry);
			if (!TryAssignVariant(ab, entry))
			{
				Debug.Log(string.Format("[ABTesting] {0} participationDraw set as holdout ({1:0.###})", ab.Name, participationDraw));
			}
		}

		private static void PromoteHoldoutIfEligible(ABDefinition ab, PersistedABEntry entry)
		{
			if (ab == null || entry == null || entry.variantId.HasValue || !ab.AreConditionsMet())
			{
				return;
			}
			ABVariantDefinition variant = AssignVariantFromDraw(ab, entry.participationDraw);
			if (variant != null)
			{
				CompleteVariantAssignment(ab, entry.participationDraw, variant, true);
			}
		}

		private static bool TryAssignVariant(ABDefinition ab, PersistedABEntry entry)
		{
			if (ab == null || entry == null || entry.variantId.HasValue || !IsParticipant(ab, entry.participationDraw))
			{
				return false;
			}
			ABVariantDefinition variant = AssignVariantFromDraw(ab, entry.participationDraw);
			if (variant == null)
			{
				return false;
			}
			CompleteVariantAssignment(ab, entry.participationDraw, variant, false);
			return true;
		}

		private static void CompleteVariantAssignment(ABDefinition ab, float participationDraw, ABVariantDefinition variant, bool holdoutPromotion)
		{
			SetEntryVariantId(ab.Name, variant.VariantID);
			string values = ab.ControlledKeyNames == null ? string.Empty : string.Join(", ", ab.ControlledKeyNames.Select((string key) => key + "=" + variant.GetValue(key)));
			Debug.Log(string.Format("[ABTesting] {0} assigned variant {1} ({2:0.###}){3}: {4}", ab.Name, variant.VariantID, participationDraw, holdoutPromotion ? " after holdout" : string.Empty, values));
		}

		private static bool IsParticipant(ABDefinition ab, float random01)
		{
			return ab != null && random01 <= Mathf.Clamp01(ab.ParticipationPercentage / 100f);
		}

		private static ABVariantDefinition AssignVariantFromDraw(ABDefinition ab, float participationDraw)
		{
			if (ab?.Variants == null || ab.Variants.Count == 0)
			{
				return null;
			}
			int index = GetVariantIndex(ab.Variants, participationDraw);
			return index >= 0 && index < ab.Variants.Count ? ab.Variants[index] : null;
		}

		private static ABVariantDefinition FindVariantById(ABDefinition ab, int variantId)
		{
			return ab?.Variants?.FirstOrDefault((ABVariantDefinition v) => v.VariantID == variantId);
		}

		private static void PersistEntry(PersistedABEntry newEntry)
		{
			if (SaveService.Data == null || newEntry == null)
			{
				return;
			}
			PersistedABEntries entries = SaveService.Data.ABEntries ?? PersistedABEntries.GetDefault();
			entries.data = entries.data ?? new List<PersistedABEntry>();
			entries.data.RemoveAll((PersistedABEntry x) => x.key == newEntry.key);
			entries.data.Add(newEntry);
			SaveService.Data.ABEntries = entries;
		}

		private static void SetEntryVariantId(string abName, int variantId)
		{
			PersistedABEntries entries = SaveService.Data?.ABEntries;
			if (entries?.data == null)
			{
				return;
			}
			entries.data = entries.data.Select((PersistedABEntry entry) => entry.key == abName ? new PersistedABEntry(entry.key, entry.participationDraw, variantId) : entry).ToList();
			SaveService.Data.ABEntries = entries;
		}

		public static int GetVariantIndex(List<ABVariantDefinition> variants, float random01)
		{
			if (variants == null || variants.Count == 0)
			{
				return 0;
			}
			float total = variants.Sum((ABVariantDefinition v) => Mathf.Max(0f, v.Ratio));
			if (total <= 0f)
			{
				return 0;
			}
			float draw = Mathf.Clamp01(random01) * total;
			float cumulative = 0f;
			for (int i = 0; i < variants.Count; i++)
			{
				cumulative += Mathf.Max(0f, variants[i].Ratio);
				if (draw <= cumulative)
				{
					return i;
				}
			}
			return variants.Count - 1;
		}

		public static string GetABDataForAnalytics()
		{
			return string.Join(",", ABLibrary.ABDefs.Select((ABDefinition ab) => (ab.Name, Variant: GetPersistedVariant(ab))).Where((x) => x.Variant != null).Select((x) => string.Format("{0}:{1}", x.Name, x.Variant.VariantID)));
		}

		public static List<(string, string)> GetABValuesForAdjust()
		{
			return ABLibrary.ABDefs.Select((ABDefinition ab) => (ab.Name, Variant: GetPersistedVariant(ab))).Where((x) => x.Variant != null).Select((x) => (x.Name, x.Variant.VariantID.ToString())).ToList();
		}

		public static bool IsUserInVariant(string abName)
		{
			ABDefinition ab = ABLibrary.GetABForName(abName);
			return ab != null && GetPersistedVariant(ab) != null;
		}
	}
}
