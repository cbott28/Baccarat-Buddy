using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Baccarat_Buddy
{
    public partial class MainPage : ContentPage
    {
        private decimal _unit;
        private decimal _startingBet;
        private decimal _sessionBankroll;
        private Shoe _shoe;

        public MainPage()
        {
            InitializeComponent();
            SessionInfoGrid.IsVisible = false;
            NextBetHeaderLabel.IsVisible = false;
            NextBetLabel.IsVisible = false;
            PlayerBtn.IsEnabled = false;
            TieBtn.IsEnabled = false;
            BankerBtn.IsEnabled = false;

            List<string> availableUnits = new List<string>();

            for (int i = 1; i <= 100; i++)
            {
                availableUnits.Add(i.ToString());
            }

            StartingUnitPicker.ItemsSource = availableUnits;
            _shoe = new Shoe();
        }

        void StartingUnitSelected(object sender, EventArgs e)
        {
            _unit = decimal.Parse(StartingUnitPicker.SelectedItem.ToString());
            _startingBet = _unit * 3;
            PlayerBtn.IsEnabled = true;
            TieBtn.IsEnabled = true;
            BankerBtn.IsEnabled = true;
        }

        async void PlayerButtonClicked(object sender, EventArgs e)
        {
            bool hasConfirmation = await DisplayAlert("Confirmation", "You've chosen PLAYER as the last outcome.", "Continue", "Cancel");

            if (hasConfirmation)
            {
                initializeView();
                updateShoe(Outcome.Player);
                updateDisplay();
            }
        }

        async void TieButtonClicked(object sender, EventArgs e)
        {
            bool hasConfirmation = await DisplayAlert("Confirmation", "You've chosen TIE as the last outcome.", "Continue", "Cancel");

            if (hasConfirmation)
            {
                initializeView();
                updateShoe(Outcome.Tie);
                updateDisplay();
            }
        }

        async void BankerButtonClicked(object sender, EventArgs e)
        {
            bool hasConfirmation = await DisplayAlert("Confirmation", "You've chosen BANKER as the last outcome.", "Continue", "Cancel");

            if (hasConfirmation)
            {
                initializeView();
                updateShoe(Outcome.Banker);
                updateDisplay();
            }
        }

        private void initializeView()
        {
            StartingUnitPicker.IsEnabled = false;
            SessionInfoGrid.IsVisible = true;
            NextBetHeaderLabel.IsVisible = true;
            NextBetLabel.IsVisible = true;
        }

        private void updateShoe(Outcome outcome)
        {
            if (outcome == Outcome.Tie)
                _sessionBankroll -= _shoe.LastBetAmount;
            else
            {
                _shoe.Hands.Add(outcome);
                Result result = getResult(outcome);
                updateShoeBankroll(result);
                _sessionBankroll += (result == Result.Win) ? _shoe.LastBetAmount : _shoe.LastBetAmount * -1;

                if (result == Result.Loss &&
                    _shoe.LastBetAmount >= _shoe.HighestBetAmountLost &&
                    !_shoe.IsInContinuationPattern)
                    _shoe.HighestBetAmountLost = _shoe.LastBetAmount;
                else if (result == Result.Win &&
                    _shoe.HighestBetAmountLost > _startingBet &&
                    _shoe.LastBetAmount >= _shoe.HighestBetAmountLost)
                {
                    _shoe.HighestBetAmountLost -= _unit;

                    if (_shoe.HighestBetAmountLost == _startingBet)
                        _shoe.HighestBetAmountLost -= _unit;
                }

                if (_shoe.Hands.Count() == 3 && result == Result.Win)
                {
                    _shoe.IsInContinuationPattern = true;
                    _shoe.IsInLossRecoveryPattern = false;
                }
                else if (_shoe.Hands.Count() == 4 && result == Result.Loss && _shoe.LastResult == Result.Loss)
                {
                    _shoe.IsInLossRecoveryPattern = true;
                    _shoe.IsInContinuationPattern = false;
                }

                _shoe.LastResult = result;

                if (_shoe.Hands.Count() >= 4 && _shoe.Bankroll > _startingBet &&
                    !_shoe.IsInContinuationPattern && _shoe.HighestBetAmountLost <= _startingBet)
                    _shoe = new Shoe();
                else if (_shoe.Hands.Count() == 4 && result == Result.Win &&
                         !_shoe.IsInContinuationPattern && _shoe.Bankroll > 0m &&
                         _shoe.HighestBetAmountLost <= _startingBet)
                {
                    _shoe = new Shoe();
                }
                else if (_shoe.IsInContinuationPattern && result == Result.Loss &&
                         _shoe.Bankroll >= _startingBet && _shoe.HighestBetAmountLost <= _startingBet)
                    _shoe = new Shoe();
                else if ((_shoe.IsInContinuationPattern && result == Result.Loss) ||
                         (_shoe.IsInLossRecoveryPattern && _shoe.Hands.Count() >= 5 && result == Result.Loss) ||
                         (_shoe.Hands.Count() == 4 && result == Result.Win && !_shoe.IsInContinuationPattern))
                {
                    _shoe.Hands.Clear();
                    _shoe.Hands.Add(outcome);
                    _shoe.IsInContinuationPattern = false;
                    _shoe.IsInLossRecoveryPattern = false;
                }

                Bet bet = getNextBet(result);
                _shoe.LastBetAmount = bet.Amount;
                _shoe.LastOutcomeChoice = bet.Outcome;
            }
        }

        private Result getResult(Outcome outcome)
        {
            if (_shoe.Hands.Count() <= 2)
                return Result.Ignore;

            if (_shoe.LastOutcomeChoice != outcome)
                return Result.Loss;

            return Result.Win;
        }

        private void updateShoeBankroll(Result result)
        {
            if (result == Result.Win)
                _shoe.Bankroll += _shoe.LastBetAmount;
            else if (result == Result.Loss)
                _shoe.Bankroll -= _shoe.LastBetAmount;
        }

        private Bet getNextBet(Result result)
        {
            Bet bet = new Bet();

            if (_shoe.Hands.Count() < 2)
                return bet;
            else
            {
                bet.Outcome = getNextOutcome(result);
                bet.Amount = getNextAmount(result);
            }

            return bet;
        }

        private Outcome getNextOutcome(Result result)
        {
            if (_shoe.Hands.Count() == 2)
                return _shoe.Hands.First() == Outcome.Banker ? Outcome.Player : Outcome.Banker;
            else if (_shoe.Hands.Take(2).Distinct().Count() == 1)
            {
                if (_shoe.Hands.Count() == 3)
                    return _shoe.LastOutcomeChoice;
                else if (!_shoe.IsInContinuationPattern)
                    return _shoe.Hands.First();
                else if (_shoe.Hands.Count() % 2 == 0)
                    return (_shoe.LastOutcomeChoice == Outcome.Banker) ? Outcome.Player : Outcome.Banker;
                else
                    return (_shoe.LastOutcomeChoice == Outcome.Banker) ? Outcome.Banker : Outcome.Player;
            }
            else
            {
                if (_shoe.Hands.Count() == 3 ||
                    (!_shoe.IsInContinuationPattern && _shoe.Hands.Count >= 5))
                    return (_shoe.LastOutcomeChoice == Outcome.Banker) ? Outcome.Player : Outcome.Banker;
                else if (!_shoe.IsInContinuationPattern && _shoe.Hands.Count() == 4)
                {
                    return _shoe.LastOutcomeChoice;
                }
                else if (_shoe.Hands.Count() % 3 != 0)
                    return (_shoe.Hands.First() == Outcome.Banker) ? Outcome.Player : Outcome.Banker;
                else
                    return (_shoe.LastOutcomeChoice == Outcome.Banker) ? Outcome.Player : Outcome.Banker;
            }
        }

        private decimal getNextAmount(Result result)
        {
            if (_shoe.Hands.Count == 2)
            {
                if (_shoe.HighestBetAmountLost == 0m ||
                    _shoe.HighestBetAmountLost <= _startingBet)
                    return _startingBet;

                return _shoe.HighestBetAmountLost + _unit;
            }
            else if (_shoe.Hands.Count() == 3 && _shoe.IsInContinuationPattern)
                return _unit;
            else if (_shoe.IsInContinuationPattern && result == Result.Win ||
                     _shoe.IsInLossRecoveryPattern && result == Result.Loss ||
                     (_shoe.HighestBetAmountLost > _startingBet && result == Result.Loss))
                return _shoe.LastBetAmount + _unit;
            else if (!_shoe.IsInContinuationPattern && _shoe.Hands.Count() == 3)
                return _shoe.LastBetAmount + (_shoe.IsInLossRecoveryPattern ? _unit : _unit * 2);
            else if (_shoe.IsInLossRecoveryPattern && result == Result.Win)
            {
                if (_shoe.LastBetAmount - _unit == _startingBet + _unit)
                    return _shoe.LastBetAmount - (_unit * 2);
                else
                    return _shoe.LastBetAmount - _unit;
            }

            return 0m;
        }

        private void updateDisplay()
        {
            BankrollLabel.Text = _sessionBankroll.ToString("C");
            HandNumberLabel.Text = _shoe.Hands.Count().ToString();

            if (_shoe.LastBetAmount == 0m)
                NextBetLabel.TextColor = Color.Black;
            else if (_shoe.LastOutcomeChoice == Outcome.Player)
                NextBetLabel.TextColor = Color.Blue;
            else
                NextBetLabel.TextColor = Color.Red;

            if (_shoe.LastBetAmount > 0m)
                NextBetLabel.Text = string.Format("Place {0} on {1}", _shoe.LastBetAmount.ToString("C"), _shoe.LastOutcomeChoice.ToString());
            else
                NextBetLabel.Text = "Ignore next hand";
        }
    }
}
