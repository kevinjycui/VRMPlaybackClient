using System;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

public static class SavCsv
{
    public static bool Save(string filename, List<string> header, List<List<float>> data) 
    {
		if (!filename.ToLower().EndsWith(".csv")) {
			filename += ".csv";
		}

		var filepath = Path.Combine(Application.persistentDataPath, filename);

		Debug.Log(filepath);

		// Make sure directory exists if user is saving to sub dir.
		Directory.CreateDirectory(Path.GetDirectoryName(filepath));

		File.WriteAllText(filepath, string.Join(",", header.ToArray()) + Environment.NewLine);
        File.AppendAllLines(filepath, data.Select(line => string.Join(",", line.ConvertAll<string>(x => x.ToString()).ToArray())));

		return true; // TODO: return false if there's a failure saving the file
	}

	public static List<List<float>> Load(string filename)
	{
		if (!filename.ToLower().EndsWith(".csv")) {
			filename += ".csv";
		}

		var filepath = Path.Combine(Application.persistentDataPath, filename);

		Debug.Log(filepath);

		List<List<float>> data = File.ReadAllLines(filepath).Skip(1).Select(
			v => Array.ConvertAll(v.Split(','), x => float.TryParse(x, out float number) ? number : 0).ToList()).ToList();
		return data;
	}

}