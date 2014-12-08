using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Speech.Recognition;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Timer = System.Timers.Timer;


namespace SpeechControlledTimer
{
    public sealed class CounterViewModel : INotifyPropertyChanged
    {
        #region Fields <>---------------------------------------------------------------------------------
        /// <summary>
        /// object for time messarument
        /// </summary>
        private readonly Timer _timer;
        /// <summary>
        /// Timestamp for difference calculations, related to trainings counter
        /// </summary>
        private DateTime _currentTime;
        /// <summary>
        /// Timestamp for difference calculations, related to break counter
        /// </summary>
        private DateTime _pausedTime;
        /// <summary>
        /// last delta of trainings counter  
        /// </summary>
        private TimeSpan _lastSpan;
        /// <summary>
        /// all breaks aggregated
        /// </summary>
        private TimeSpan _breakTimeSpan;
        /// <summary>
        /// object for speech recognition
        /// </summary>
        private readonly SpeechRecognizer _recognizer;
        /// <summary>
        /// program state machine 
        /// </summary>
        private RunStates _state;

        #region Dependency Attributes (write only via Property) <>----------------------------------------
        private string __trainingsTime;
        private string __breakTime;
        private string __totalBreakTime;
        #endregion

        #region ICommand Objects <>-----------------------------------------------------------------------

        private readonly IRelayCommand __startCommand;
        private readonly IRelayCommand __stopCommand;
        private readonly IRelayCommand __breakOrResumeCommand;
        private readonly IRelayCommand __defaultCommand = new RelayCommand(cmdObj => { });

        #endregion

        #region Constants and Enums <>--------------------------------------------------------------------
        
        private enum RunStates
        {
            Stopped = 0,
            Running = 1,
            Breaked = 2
        };

        private const string _KeywordStart = "start";
        private const string _KeywordStop = "stop";
        private const string _KeywordBreakGer = "pause";
        private const string _KeywordBreak = "break";
        private const string _KeywordResumeGer = "weiter";
        private const string _KeywordResume = "resume";

        #endregion

        #endregion

        #region Constructor <>----------------------------------------------------------------------------

        public CounterViewModel()
        {
            #region Dependency Properties Setup <>--------------------------------------------------------
            const string defaultTimeString = "00:00:00";

            TrainingsTime = defaultTimeString;
            BreakTime = defaultTimeString;
            TotalBreakTime = defaultTimeString;

            _state = RunStates.Stopped;
            #endregion

            #region Commands Setups <>--------------------------------------------------------------------
            __startCommand = new RelayCommand(
                cmdObj =>
                {
                    _currentTime = DateTime.Now;
                    _timer.Start();

                    SetProgramState(RunStates.Running);
                },
                cmdObj => _state == RunStates.Stopped
            );

            __stopCommand = new RelayCommand(
                cmdObj =>
                {
                    _timer.Stop();

                    SetProgramState(RunStates.Stopped);
                },
                cmdObj => _state == RunStates.Running || _state == RunStates.Breaked
            );

            __breakOrResumeCommand = new RelayCommand(
                cmdObj =>
                {
                    if (_state == RunStates.Breaked)
                    {
                        //save break timespan for total break time
                        _breakTimeSpan += DateTime.Now - _pausedTime;
                        //reset time for training counter
                        _currentTime = DateTime.Now.Subtract(_lastSpan);

                        SetProgramState(RunStates.Running);
                        IsPaused = false;
                    }
                    else
                    {
                        _lastSpan = DateTime.Now - _currentTime;

                        _pausedTime = DateTime.Now;

                        SetProgramState(RunStates.Breaked);
                        IsPaused = true;
                    }
                },
                cmdObj => _state == RunStates.Running || _state == RunStates.Breaked
            );

            
            #endregion

            #region Timer Setup <>------------------------------------------------------------------------
            _timer = new Timer(1000);
            _timer.Elapsed += (sender, args) =>
            {
                if (_state == RunStates.Stopped) throw new InvalidOperationException("invalid programe state, got timer event but not in running state");

                if (_state == RunStates.Breaked)
                {//paused
                    BreakTime = GetNewTimestampText(args.SignalTime, _pausedTime);
                    TotalBreakTime = GetNewTimestampText(args.SignalTime, _pausedTime.Subtract(_breakTimeSpan));
                }
                else
                {//running
                    TrainingsTime = GetNewTimestampText(args.SignalTime, _currentTime);
                }
            };
            #endregion

            #region Speech Recognizer Setup <>------------------------------------------------------------
            _recognizer = new SpeechRecognizer();
            _recognizer.SpeechRecognized += (sender, args) =>
            {
                Debug.WriteLine("Speech recognized: {0}", args.Result.Text);

                ICommand command;
                switch (args.Result.Text.ToLower(Thread.CurrentThread.CurrentCulture))
                {
                    case _KeywordStart:
                        command = StartCommand;
                        break;
                    case _KeywordStop:
                        command = StopCommand;
                        break;
                    case _KeywordResume:
                    case _KeywordResumeGer:
                    case _KeywordBreak:
                    case _KeywordBreakGer:
                        command = BreakOrResumeCommand;
                        break;
                    default: 
                        command = __defaultCommand;
                        break;
                }

                if (command.CanExecute(this))
                {
                    command.Execute(this);
                }
            };

            var choices = new Choices(new[] 
            { 
                _KeywordStart, 
                _KeywordStop, 
                _KeywordBreak, 
                _KeywordBreakGer, 
                _KeywordResume, 
                _KeywordResumeGer 
            });

            var grammarBuilder = new GrammarBuilder(choices);
            var grammar = new Grammar(grammarBuilder);
            _recognizer.LoadGrammarAsync(grammar);
            _recognizer.Enabled = true;
            #endregion

        }
        #endregion

        #region Dependency Properties <>------------------------------------------------------------------

        public bool IsPaused
        {
            get
            {
                return _state == RunStates.Breaked;
            }
            set
            {
                OnPropertyChanged();
            }
        }

        public string TrainingsTime
        {
            get
            {
                return __trainingsTime;
            }
            set
            {
                if (__trainingsTime == value) return;

                __trainingsTime = value;
                OnPropertyChanged();
            }
        }

        public string BreakTime
        {
            get
            {
                return __breakTime;
            }
            set
            {
                if (__breakTime == value) return;

                __breakTime = value;
                OnPropertyChanged();
            }
        }

        public string TotalBreakTime
        {
            get
            {
                return __totalBreakTime;
            }
            set
            {
                if (__totalBreakTime == value) return;

                __totalBreakTime = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region View Model API <>-------------------------------------------------------------------------

        public ICommand StartCommand
        {
            get { return __startCommand; }
        }

        public ICommand StopCommand
        {
            get { return __stopCommand; }
        }

        public ICommand BreakOrResumeCommand
        {
            get { return __breakOrResumeCommand; }
        }

        #endregion

        #region Private Implementation <>-----------------------------------------------------------------

        private string GetNewTimestampText(DateTime actualTime, DateTime referenceTime)
        {
            var timeDifference = actualTime - referenceTime;
            return timeDifference.ToString(@"hh\:mm\:ss", Thread.CurrentThread.CurrentCulture);
        }

        private void SetProgramState(RunStates state)
        {
            _state = state;
            CallCommandCanExecute();
        }

        private void CallCommandCanExecute()
        {
            __startCommand.RaiseCanExecuteChanged();
            __stopCommand.RaiseCanExecuteChanged();
            __breakOrResumeCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region INotifyPropertyChanged <>-----------------------------------------------------------------

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string callerName = null)
        {
            if (string.IsNullOrWhiteSpace(callerName)) throw new ArgumentNullException("callerName");
            InternalPropertyChanged(callerName);
        }

        private void InternalPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
        #endregion
    }
}
