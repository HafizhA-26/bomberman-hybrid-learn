using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace BombermanRL.UI
{
    [RequireComponent(typeof(TMP_InputField))]
    public class InputNameValidator : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _invalidText;

        [Header("Config")]
        [SerializeField] private TextAsset _bannedWordData;
        [SerializeField] private int _maxLength = 8;
        [SerializeField] private bool _enabledCheckValueChanged = true;
        [SerializeField] private float _inputDebounce = 0.3f;

        private TMP_InputField _input;
        private HashSet<string> bannedSet;
        private Regex _alphaNumRegex;
        private Coroutine debounceRoutine;
        public ValidationResult Result { get; private set; }
        public TMP_InputField Input { get => _input; }

        [Serializable]
        private class BannedWrapper { public string[] words; }
        [Serializable]
        public enum ValidationResult { Ok, Empty, TooLong, InvalidChars, BannedWord, UsedUsername }
        private void Awake()
        {
            _input = GetComponent<TMP_InputField>();
            _alphaNumRegex = new Regex("^[A-Za-z0-9]+$", RegexOptions.Compiled);
            Result = Validate("");

            LoadBannedWords();
            _input.onEndEdit.AddListener(OnEndEdit);
            if(_enabledCheckValueChanged) _input.onValueChanged.AddListener(OnInputNameChanged);
        }

        private void OnDestroy()
        {
            _input.onEndEdit.RemoveListener(OnEndEdit);
            if (_enabledCheckValueChanged) _input.onValueChanged.RemoveListener(OnInputNameChanged);
        }

        public void OnEndEdit(string text) 
        {
            Result = Validate(text);
        }

        private void OnInputNameChanged(string text)
        {
            if (debounceRoutine != null) StopCoroutine(debounceRoutine);
            debounceRoutine = StartCoroutine(DebounceValidate());
        }

        private IEnumerator DebounceValidate()
        {
            yield return new WaitForSecondsRealtime(_inputDebounce);
            Result = Validate(_input.text, true);
        }

        /// <summary>
        /// Load and convert banned words data form into HashSet
        /// </summary>
        private void LoadBannedWords()
        {
            bannedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (_bannedWordData == null || string.IsNullOrWhiteSpace(_bannedWordData.text))
                return;

            try
            {
                // Convert banned word json into HashSet to faster validation
                string wrapped = "{\"words\":" + _bannedWordData.text + "}";

                BannedWrapper wrapper = JsonConvert.DeserializeObject<BannedWrapper>(wrapped);
                Debug.Log("Converted length" + wrapper.words.Length);
                if (wrapper != null && wrapper.words != null)
                {
                    foreach (var w in wrapper.words)
                    {
                        if(string.IsNullOrEmpty(w)) continue;
                        bannedSet.Add(Normalize(w));
                    }
                }
            }catch (Exception e)
            {
                Debug.Log(e);
                Debug.LogError($"Failed to parse banned word data: {e.Message}");
            }
        }

        /// <summary>
        /// Main function to validate all criteria for name
        /// </summary>
        /// <param name="raw">Raw Name Input Value</param>
        /// <param name="silent">Should not trigger warning?</param>
        /// <returns>Validation Result for name input</returns>
        public ValidationResult Validate(string raw, bool silent = false)
        {
            string value = raw ?? string.Empty;
            value = value.Trim();

            // Validation length check
            if(value.Length == 0)
            {
                if (!silent) _invalidText.text = "Name cannot be empty";
                return ValidationResult.Empty;
            }
            if(value.Length > _maxLength)
            {
                if (!silent) _invalidText.text = $"Name exceed {_maxLength} character";
                return ValidationResult.TooLong;
            }

            // Alphanumeric check
            if (!_alphaNumRegex.IsMatch(value))
            {
                if (!silent) _invalidText.text = "Name has Invalid Characters";
                return ValidationResult.InvalidChars;
            }

            // Banned word check [EXACT MATCH]
            string normed = Normalize(value);
            if(bannedSet.Contains(normed))
            {
                if (!silent) _invalidText.text = "Inappropriate Name";
                return ValidationResult.BannedWord;
            }

            // Banned word check [SUBSTRING/LIKELY MATCH]
            foreach (string banned in bannedSet)
            {
                if (banned.Length >= 2 && normed.Contains(banned))
                {
                    if(!silent) _invalidText.text = "Inappropriate Name";
                    return ValidationResult.BannedWord;
                }
            }

            return ValidationResult.Ok;
        }

        /// <summary>
        /// Process word lowercase, decompose, remove diacritics, keep only ascii letters & digits
        /// </summary>
        /// <param name="s">Word to check</param>
        /// <returns>Normalized string</returns>
        private static string Normalize(string s)
        {
            if(string.IsNullOrEmpty(s)) return s;
            string lower = s.ToLowerInvariant();
            string form = lower.Normalize(System.Text.NormalizationForm.FormKD);

            StringBuilder sb = new StringBuilder(form.Length);
            foreach (char c in form)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if(uc != UnicodeCategory.NonSpacingMark) sb.Append(c);
            }

            string cleanedStr = sb.ToString();
            StringBuilder sb2 = new StringBuilder(cleanedStr.Length);
            foreach (char c in cleanedStr)
            {
                if((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')) sb2.Append(c);
            }
            return sb2.ToString();
        }

        public void SetUsernameTakenState()
        {
            Result = ValidationResult.UsedUsername;
            _invalidText.gameObject.SetActive(true);
            _invalidText.text = "Username Already Taken";
        }
    }
}
