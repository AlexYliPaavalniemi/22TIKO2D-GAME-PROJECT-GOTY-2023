using System;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    /// <summary>Stats for the player.</summary>
    public static class PlayerStats
    {
        /// <summary>Good experience collected by the player.</summary>
        public static uint GoodExp { get; set; } = 0;

        /// <summary>Bad experience collected by the player.</summary>
        public static uint BadExp { get; set; } = 0;

        /// <summary>Total experience collected by the player.</summary>
        public static uint TotalExp => GoodExp + BadExp;

        /// <summary>Player's position on the map.</summary>
        public static Vector2 Position { get; set; } = Vector2.zero;

        /// <summary>Maximum health of the player character.</summary>
        public static uint MaxHealth => 100 - BadExp / 2;

        /// <summary>The amount of damage player does to enemies.</summary>
        public static uint Damage => 10 + GoodExp;

        /// <summary>The speed of the player.</summary>
        public static uint Speed => 4 + GoodExp / 20;

        /// <summary>Amount of health to restore when player heals.</summary>
        public static uint HealAmount => MaxHealth / 5;

        /// <summary>Power of the player compared to the difficulty of the enemies.</summary>
        public static decimal Power => Damage * Speed * MaxHealth;

        /// <summary>Skills the player possesses.</summary>
        public static List<string> Skills = new List<string>();

        /// <summary>A list of the skill classes.</summary>
        public static List<Battle.IPlayerSkill> SkillClasses =>
            Skills
                .Select(
                    // Get the skill by its name.
                    (skill) =>
                        (Battle.IPlayerSkill)
                            Activator.CreateInstance(Type.GetType($"Battle.Skill.{skill}"))
                )
                .ToList();

        /// <summary>Unlock a new skill.</summary>
        public static bool UnlockSkill(string name)
        {
            // Add the skill if it doesn't yet exist.
            if (!Skills.Contains(name))
            {
                Skills.Add(name);

                // Sort the skills.
                Skills.Sort();

                return true;
            }

            return false;
        }

        /// <summary>Data structure used for saving the player stats.</summary>
        [Serializable]
        private struct SaveData
        {
            public uint goodExp;
            public uint badExp;
            public Vector2 position;
            public string[] skills;
        }

        /// <summary>Reset the player stats to their default values.</summary>
        public static void Reset()
        {
            GoodExp = 0;
            BadExp = 0;
            Position = Vector2.zero;
            Skills = new List<string>();
        }

        /// <summary>Save file's name.</summary>
        private const string SaveFile = "player.sav";

        /// <summary>Save the player stats.</summary>
        public static void Save()
        {
            // Use AES encryption to save the JSON to a file.
            // Might be an overkill in this situation,
            // especially as we put the encryption key to PlayerPrefs,
            // which allows easy circumvention of the encryption.
            // Nonetheless this should make it a bit harder to edit the save file by hand.
            using (Aes aes = Aes.Create())
            using (FileStream fs = File.Create(Path.Join(Application.persistentDataPath, SaveFile)))
            using (
                CryptoStream crypto = new CryptoStream(
                    fs,
                    aes.CreateEncryptor(aes.Key, aes.IV),
                    CryptoStreamMode.Write
                )
            )
            using (StreamWriter writer = new StreamWriter(crypto))
            {
                // Write the initialization vector to the start of the file.
                fs.Write(aes.IV, 0, aes.IV.Length);

                // Get the values.
                SaveData saveData = new SaveData
                {
                    goodExp = GoodExp,
                    badExp = BadExp,
                    position = Position,
                    skills = Skills.ToArray(),
                };

                // Write to the file.
                writer.Write(JsonUtility.ToJson(saveData));

                // Save the key to PlayerPrefs.
                PlayerPrefs.SetString("SaveKey", System.Convert.ToBase64String(aes.Key));
                PlayerPrefs.Save();
            }
        }

        /// <summary>Load the player stats.</summary>
        public static void Load()
        {
            using (Aes aes = Aes.Create())
            using (
                FileStream fs = File.OpenRead(Path.Join(Application.persistentDataPath, SaveFile))
            )
            {
                byte[] IV = new byte[aes.IV.Length];
                // Read the initialization vector from the beginning of the file.
                fs.Read(IV, 0, IV.Length);

                // Get the encryption key from the PlayerPrefs.
                byte[] key = System.Convert.FromBase64String(PlayerPrefs.GetString("SaveKey"));

                using (
                    CryptoStream crypto = new CryptoStream(
                        fs,
                        aes.CreateDecryptor(key, IV),
                        CryptoStreamMode.Read
                    )
                )
                using (StreamReader reader = new StreamReader(crypto))
                {
                    // Read the file.
                    SaveData saveData = JsonUtility.FromJson<SaveData>(reader.ReadToEnd());

                    // Set the values.
                    GoodExp = saveData.goodExp;
                    BadExp = saveData.badExp;
                    Position = saveData.position;
                    Skills = new List<string>(saveData.skills);
                }
            }
        }

        /// <summary>Wether the save file exists or not.</summary>
        public static bool SavePresent =>
            File.Exists(Path.Join(Application.persistentDataPath, SaveFile));
    }
}
