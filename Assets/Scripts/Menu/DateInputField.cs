using System;
using TMPro;
using UnityEngine;

namespace Menu
{
    public class DateInputField : MonoBehaviour
    {
        private TMP_InputField _inputField;

        private int _year=0, _month=0, _day=0;

        public Action OnDateSet;

        private void Awake()
        {
            _inputField = GetComponent<TMP_InputField>();
        }

        private void OnEnable()
        {
            _inputField.onValidateInput += ValidateTimeBegin;
            _inputField.onEndEdit.AddListener(CheckInput);
        }

        private void OnDisable()
        {
            _inputField.onValidateInput -= ValidateTimeBegin;
            _inputField.onEndEdit.RemoveListener(CheckInput);

        }
        
        private void CheckInput(string arg0)
        {
            _day = 0;
            _month = 0;
            _year = 0;
            if (arg0.Length >= 12)
            {
                int index = 0;
                for (int i = 0; i < arg0.Length; i++)
                {
                    if (char.IsDigit(arg0[i]))
                    {
                        int len;
                        switch (index)
                        {
                            case 0:
                                len = 1;
                                if (char.IsDigit(arg0[i + 1]))
                                {
                                    len++;
                                }

                                if (int.TryParse(arg0.Substring(i, len), out _day))
                                {
                                    i += len;
                                }
                                break;
                            
                            case 1:
                                len = 1;
                                if (char.IsDigit(arg0[i + 1]))
                                {
                                    len++;
                                }

                                if (int.TryParse(arg0.Substring(i, len), out _month))
                                {
                                    i += len;
                                }
                                break;
                            case 2:
                                len = 4;
                                if (i + len <= arg0.Length)
                                {
                                    if (int.TryParse(arg0.Substring(i, len), out _year))
                                    {
                                        break;
                                    }
                                }
                                
                                
                                break;
                        }

                        index++;
                    }
                }
            }
            
            // Debug.Log(GetDateTime());
            
            if(_day!=0 && _month!=0 && _year!=0)
                OnDateSet?.Invoke();
        }
        
        private bool allSlashAdded = false;

        private char ValidateTimeBegin(string text, int charindex, char addedchar)
        {
            // Debug.Log("text: " + text + ", charIndex: " + charindex + ", addedChar: " + addedchar);

            if (text.Length >= 14) return '\0';

            bool isDay = text.Length == 1;
            bool isMonth = text.Length == 6;
     
            if (!allSlashAdded && (isDay || isMonth) && char.IsDigit(addedchar))
            {
                allSlashAdded = text.Length == 7;
 
                _inputField.text = text + addedchar + " / ";
                
                _inputField.stringPosition = _inputField.text.Length;

                // if (isDay)
                // {
                //     string day = _inputField.text.Substring(0, 2);
                //
                //     if (int.TryParse(day, out _day))
                //     {
                //         
                //     }
                // }
                // else
                // {
                //     string month = _inputField.text.Substring(5, 2);
                //
                //     if (int.TryParse(month, out _month))
                //     {
                //         
                //     }
                // }
 
                return '\0';
            }

            // if (_inputField.text.Length >= 14)
            // {
            //     string year = _inputField.text.Substring(_inputField.text.Length - 5, 4);
            //
            //     if (int.TryParse(year, out _year))
            //     {
            //     }
            // }
 
            // Debug.Log(addedchar + " is digit : " + char.IsDigit(addedchar));
 
            return char.IsDigit(addedchar) ? addedchar : '\0';
        }

        public void SetDateTime(DateTime dateTime)
        {
            if(_inputField==null)
                _inputField = GetComponent<TMP_InputField>();
            _day = dateTime.Day;
            _month = dateTime.Month;
            _year = dateTime.Year;
            
            _inputField.SetTextWithoutNotify($"{_day} / {_month} / {_year}");
        }

        public DateTime GetDateTime()
        {
            if (_day == 0 || _month ==0 || _year==0) return DateTime.Today;

            return new DateTime(_year, _month, _day);
        }
    }
}