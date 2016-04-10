////////////////////////////////////////////////////////////////////////////
// View-Model for T9App
// 
// Description: This class contains all the logic behing the MainWindow view
//              including bind-able text display, predictive mode flag, and
//              commands for when buttons are pressed.
// Author: Benjamin Russell (brr1922)
////////////////////////////////////////////////////////////////////////////

using System;
using System.ComponentModel;
using System.Timers;
using System.Windows;
using System.Windows.Input;

namespace T9App2
{
    internal class ViewModel : INotifyPropertyChanged
    {

        #region UI-Bound Properties

        /// <summary>
        /// The temporary text for the pending word/character
        /// </summary>
        private string _tempText;
        private string TempText
        {
            get { return _tempText; }
            set
            {
                _tempText = value;
                OnPropertyChanged("TypedText");
            }
        }

        /// <summary>
        /// The text that has been completely typed out
        /// </summary>
        private string _typedText;
        public string TypedText
        {
            get { return _typedText + _tempText; }
            private set
            {
                _typedText = value;
                OnPropertyChanged("TypedText");
            }
        }

        /// <summary>
        /// Whether or not predictive mode is turned on
        /// </summary>
        private bool _predictiveMode;
        public bool PredictiveMode
        {
            get { return _predictiveMode; }
            set
            {
                _predictiveMode = value;
                TempText = "";
                TypedText = "";         // Clearing the text is kinda helpful here
                _predictiveHandler.Regex = null;
            }
        }

        #endregion

        #region Member Variables

        /// <summary>
        /// The timer for determining when to save a key press
        /// </summary>
        private readonly Timer _keyTimer;

        /// <summary>
        /// The model for handling predictive typing
        /// </summary>
        private readonly PredictiveHandler _predictiveHandler;

        /// <summary>
        /// The parameter of the key that was last pressed
        /// </summary>
        private string _lastPressedKey;

        /// <summary>
        /// The index into the list of characters from the last key pressed
        /// that is currently being displayed.
        /// </summary>
        private int _lastPressedIndex;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructs a new ViewModel. Initializes all the internal properties
        /// as well as initializes the predictive lookup tool
        /// </summary>
        public ViewModel()
        {
            // Initialize the model
            _predictiveHandler = new PredictiveHandler();
            _keyTimer = new Timer(1000) {AutoReset = false};
            _keyTimer.Elapsed += TimerElapsed;
            _keyTimer.Enabled = false;

            // Set handlers
            ButtonCommand = new ViewCommand {Action = ButtonClick};
            CopyTextCommand = new ViewCommand {Action = CopyText};
        }

        #endregion

        #region Property Changing Implementations

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        #endregion

        #region Actions

        public ICommand ButtonCommand { get; set; }
        /// <summary>
        /// The action to be taken when a button on the T9 grid is pressed.
        /// </summary>
        /// <param name="obj">The parameter passed into the command</param>
        public void ButtonClick(object obj)
        {
            if (obj is ActionEnum)
            {
                ActionEnum enumValue = (ActionEnum) obj;
                switch(enumValue)
                {
                    case ActionEnum.Backspace:
                        if (!PredictiveMode && TypedText.Length > 0)
                        {
                            // Delete a character if there are characters to delete
                            if (_keyTimer.Enabled)
                            {
                                TempText = String.Empty;
                                _keyTimer.Enabled = false;
                            }
                            else
                            {
                                TypedText = TypedText.Substring(0, TypedText.Length - 1);
                            }
                        } 
                        else if(PredictiveMode)
                        {
                            // Is there temporary text?
                            if (TempText.Length > 0)
                            {
                                // Erase the last regex and get a new word
                                _predictiveHandler.DropLastRegex();
                                if (_predictiveHandler.Regex.Length > 0)
                                {
                                    TempText = _predictiveHandler.NextMatch();
                                }
                                else
                                {
                                    TempText = String.Empty;
                                }

                            }
                            else
                            {
                                if (TypedText[TypedText.Length - 1] == ' ')
                                {
                                    // Delete the last word
                                    TypedText = TypedText.TrimEnd();
                                    int endOfLastWord = TypedText.LastIndexOf(' ');
                                    TypedText = TypedText.Substring(0, endOfLastWord + 1);
                                }
                            }
                        }
                        break;
                    case ActionEnum.InsertSpace:
                        if (PredictiveMode)
                        {
                            // Append the temporary text and a space
                            AppendTempText();
                            TypedText += " ";

                            // Clear the regex
                            _predictiveHandler.Regex = null;
                        }
                        else
                        {
                            // Insert a space, clear out the last key stuff
                            AppendTempText();
                            TypedText += " ";
                            _lastPressedIndex = 0;
                            _lastPressedKey = null;
                        }
                        break;
                    case ActionEnum.NextWord:
                        // This key only does something if we're in predictive mode
                        if (PredictiveMode)
                        {
                            // Grab the next matching word
                            TempText = _predictiveHandler.NextMatch();
                        }
                        break;
                }
            }
            else
            {
                if (PredictiveMode)
                {
                    // Process the predictive key press
                    PredictiveButtonPress(obj as string);
                }
                else
                {
                    // Process the non-predictive keypress
                    NonPredictiveButtonPress(obj as string);
                }
            }
        }

        /// <summary>
        /// Action to take when a button is pressed while in predictive mode.
        /// The key's regex is added to the regex to process and the next
        /// matching word from the dictionary is selected.
        /// </summary>
        /// <param name="regex">The regex for the letters represented by the button</param>
        public void PredictiveButtonPress(string regex)
        {
            // Append the regex of the key to the predictive handler
            _predictiveHandler.Regex += regex;

            // Grab the next match for the word
            TempText = _predictiveHandler.NextMatch();
        }

        /// <summary>
        /// Processing that occurs when a T9 button is pressed in non-predictive
        /// mode. This determines if the temporary text should be incremented or
        /// a new key should be used.
        /// </summary>
        /// <param name="keyRegex">The regex represented by the button, passed in to the command</param>
        public void NonPredictiveButtonPress(string keyRegex)
        {
            // Is this the same key as the one that was last pressed?
            var lastKey = keyRegex.Trim(new[] {'[', ']'});
            if (_lastPressedKey == lastKey)
            {
                // Increment the index of the selected character
                _lastPressedIndex = (_lastPressedIndex + 1)%_lastPressedKey.Length;
            }
            else
            {
                // Change the last pressed key
                _lastPressedKey = lastKey;
                _lastPressedIndex = 0;

                // Append the temp text
                AppendTempText();
            }

            // Change the temp text
            TempText = _lastPressedKey[_lastPressedIndex].ToString();

            // Reset the timer
            _keyTimer.Enabled = false;
            _keyTimer.Enabled = true;
        }

        /// <summary>
        /// Action to take when the key timer elapses.
        /// </summary>
        /// <param name="obj">An object from the timer</param>
        /// <param name="args">The arguments of the event</param>
        /// <remarks>I have no idea what these parameters do... but they're required!</remarks>
        private void TimerElapsed(object obj, ElapsedEventArgs args)
        {
            // Step 1: Append the current temporary key and clear temp key
            AppendTempText();

            // Step 2: Clear the last pressed key
            _lastPressedKey = null;
            _lastPressedIndex = 0;
        }

        public ICommand CopyTextCommand { get; set; }
        /// <summary>
        /// Action to be taken when the copy text button is clicked. Sets
        /// the clipboard's content to the current text.
        /// </summary>
        /// <param name="obj">The command parameter</param>
        public void CopyText(object obj)
        {
            Clipboard.SetText(TypedText);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Appends the current temporary text to the typed text. This also
        /// clears out the temporary text.
        /// </summary>
        private void AppendTempText()
        {
            string temp = TempText;
            TempText = String.Empty;
            TypedText += temp;
        }

        #endregion
    }
}
