﻿/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Linq;
using OsEngine.Market.Servers;
using OsEngine.Market;
using OsEngine.Language;

/* Description
trading robot for osengine

The trend robot on Strategy PivotFloor And PriceChannel.

Buy:
1. The candle closed above the R1 level.
2. The price is above the top line of the PC.

Sell:
1. The candle closed below the S1 level.
2. The price is below the bottom line of the PC.

Exit from buy:
The trailing stop is placed at the minimum for the period specified for the trailing
stop and transferred (slides) to new price lows, also for the specified period.
Exit from sell:
The trailing stop is placed at the maximum for the period specified for the trailing
stop and is transferred (slides) to the new maximum of the price, also for the specified period.
*/

namespace OsEngine.Robots
{
    [Bot("StrategyPivotFloorAndPriceChannel")] // Instead of manually adding through BotFactory, we use an attribute to simplify the process.
    public class StrategyPivotFloorAndPriceChannel : BotPanel
    {
        // Reference to the main trading tab
        private BotTabSimple _tab;

        // Basic Settings
        private StrategyParameterString _regime;
        private StrategyParameterDecimal _slippage;
        private StrategyParameterTimeOfDay _startTradeTime;
        private StrategyParameterTimeOfDay _endTradeTime;

        // GetVolume Settings
        private StrategyParameterString _volumeType;
        private StrategyParameterDecimal _volume;
        private StrategyParameterString _tradeAssetInPortfolio;

        // Indicator Settings 
        private StrategyParameterString _pivotFloorPeriod;
        private StrategyParameterInt _pcUpLength;
        private StrategyParameterInt _pcDownLength;

        // Indicator
        private Aindicator _pivotFloor;
        private Aindicator _PC;

        // Exit Settings
        private StrategyParameterInt _trailCandlesLong;
        private StrategyParameterInt _trailCandlesShort;

        // The last value of the indicator
        private decimal _lastR1;
        private decimal _lastS1;
        private decimal _lastCentralLine;

        // The prev value of the indicator
        private decimal _prevUpPC;
        private decimal _prevDownPC;

        public StrategyPivotFloorAndPriceChannel(string name, StartProgram startProgram) : base(name, startProgram)
        {
            // Create and assign the main trading tab
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            // Basic Settings
            _regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" }, "Base");
            _slippage = CreateParameter("Slippage %", 0m, 0, 20, 1, "Base");
            _startTradeTime = CreateParameterTimeOfDay("Start Trade Time", 0, 0, 0, 0, "Base");
            _endTradeTime = CreateParameterTimeOfDay("End Trade Time", 24, 0, 0, 0, "Base");

            // GetVolume Settings
            _volumeType = CreateParameter("Volume type", "Deposit percent", new[] { "Contracts", "Contract currency", "Deposit percent" });
            _volume = CreateParameter("Volume", 20, 1.0m, 50, 4);
            _tradeAssetInPortfolio = CreateParameter("Asset in portfolio", "Prime");

            // Indicator Settings
            _pivotFloorPeriod = CreateParameter("Period", "Daily", new[] { "Daily", "Weekly" }, "Indicator");
            _pcUpLength = CreateParameter("Up Line Length", 21, 7, 48, 7, "Indicator");
            _pcDownLength = CreateParameter("Down Line Length", 21, 7, 48, 7, "Indicator");

            // Create indicator PC
            _PC = IndicatorsFactory.CreateIndicatorByName("PriceChannel", name + "PC", false);
            _PC = (Aindicator)_tab.CreateCandleIndicator(_PC, "Prime");
            ((IndicatorParameterInt)_PC.Parameters[0]).ValueInt = _pcUpLength.ValueInt;
            ((IndicatorParameterInt)_PC.Parameters[1]).ValueInt = _pcDownLength.ValueInt;
            _PC.Save();

            // Create indicator ChaikinOsc
            _pivotFloor = IndicatorsFactory.CreateIndicatorByName("PivotFloor", name + "PivotFloor", false);
            _pivotFloor = (Aindicator)_tab.CreateCandleIndicator(_pivotFloor, "Prime");
            ((IndicatorParameterString)_pivotFloor.Parameters[0]).ValueString = _pivotFloorPeriod.ValueString;
            _pivotFloor.Save();

            // Exit Settings
            _trailCandlesLong = CreateParameter("Trail Candles Long", 5, 5, 200, 5, "Exit");
            _trailCandlesShort = CreateParameter("Trail Candles Short", 5, 5, 200, 5, "Exit");

            // Subscribe to the indicator update event
            ParametrsChangeByUser += _strategyPivotFloorAndPriceChannel_ParametrsChangeByUser; ;

            // Subscribe to the candle finished event
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;

            Description = OsLocalization.Description.DescriptionLabel268;
        }

        private void _strategyPivotFloorAndPriceChannel_ParametrsChangeByUser()
        {
            ((IndicatorParameterString)_pivotFloor.Parameters[0]).ValueString = _pivotFloorPeriod.ValueString;
            _pivotFloor.Save();
            _pivotFloor.Reload();

            ((IndicatorParameterInt)_PC.Parameters[0]).ValueInt = _pcUpLength.ValueInt;
            ((IndicatorParameterInt)_PC.Parameters[1]).ValueInt = _pcDownLength.ValueInt;
            _PC.Save();
            _PC.Reload();
        }

        // The name of the robot in OsEngine
        public override string GetNameStrategyType()
        {
            return "StrategyPivotFloorAndPriceChannel";
        }

        public override void ShowIndividualSettingsDialog()
        {

        }

        // Candle Finished Event
        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            // If the robot is turned off, exit the event handler
            if (_regime.ValueString == "Off")
            {
                return;
            }

            _lastR1 = _pivotFloor.DataSeries[1].Last;

            // If there are not enough candles to build an indicator, we exit
            if (_lastR1 == 0 ||
                candles.Count <= _pcUpLength.ValueInt + 2 ||
                candles.Count <= _pcDownLength.ValueInt + 2)
            {
                return;
            }

            // If the time does not match, we leave
            if (_startTradeTime.Value > _tab.TimeServerCurrent ||
                _endTradeTime.Value < _tab.TimeServerCurrent)
            {
                return;
            }

            List<Position> openPositions = _tab.PositionsOpenAll;

            // If there are positions, then go to the position closing method
            if (openPositions != null && openPositions.Count != 0)
            {
                LogicClosePosition(candles);
            }

            // If the position closing mode, then exit the method
            if (_regime.ValueString == "OnlyClosePosition")
            {
                return;
            }

            // If there are no positions, then go to the position opening method
            if (openPositions == null || openPositions.Count == 0)
            {
                LogicOpenPosition(candles);
            }
        }

        // Opening logic
        private void LogicOpenPosition(List<Candle> candles)
        {
            // The last value of the indicator
            _lastR1 = _pivotFloor.DataSeries[1].Last;
            _lastS1 = _pivotFloor.DataSeries[4].Last;
            _lastCentralLine = _pivotFloor.DataSeries[0].Last;

            // The prev value of the indicator
            _prevUpPC = _PC.DataSeries[0].Values[_PC.DataSeries[0].Values.Count - 2];
            _prevDownPC = _PC.DataSeries[1].Values[_PC.DataSeries[1].Values.Count - 2];

            List<Position> openPositions = _tab.PositionsOpenAll;

            if (openPositions == null || openPositions.Count == 0)
            {
                decimal lastPrice = candles[candles.Count - 1].Close;

                // Slippage
                decimal _slippage = this._slippage.ValueDecimal * _tab.Securiti.PriceStep;

                // Long
                if (_regime.ValueString != "OnlyShort") // If the mode is not only short, then we enter long
                {
                    if (_lastR1 < lastPrice && lastPrice > _prevUpPC)
                    {
                        _tab.BuyAtLimit(GetVolume(_tab), _tab.PriceBestAsk + _slippage);
                    }
                }

                // Short
                if (_regime.ValueString != "OnlyLong") // If the mode is not only long, then we enter short
                {
                    if (_lastS1 > lastPrice && lastPrice < _prevDownPC)
                    {
                        _tab.SellAtLimit(GetVolume(_tab), _tab.PriceBestBid - _slippage);
                    }
                }
            }
        }

        // Logic close position
        private void LogicClosePosition(List<Candle> candles)
        {
            List<Position> openPositions = _tab.PositionsOpenAll;

            decimal _slippage = this._slippage.ValueDecimal * _tab.Securiti.PriceStep;

            for (int i = 0; openPositions != null && i < openPositions.Count; i++)
            {
                Position position = openPositions[i];

                if (position.State != PositionStateType.Open)
                {
                    continue;
                }

                if (position.Direction == Side.Buy) // If the direction of the position is long
                {
                    decimal price = GetPriceStop(Side.Buy, candles, candles.Count - 1);
                    if (price == 0)
                    {
                        return;
                    }
                    _tab.CloseAtTrailingStop(position, price, price - _slippage);
                }
                else // If the direction of the position is short
                {
                    decimal price = GetPriceStop(Side.Sell, candles, candles.Count - 1);
                    if (price == 0)
                    {
                        return;
                    }
                    _tab.CloseAtTrailingStop(position, price, price + _slippage);
                }
            }
        }

        private decimal GetPriceStop(Side side, List<Candle> candles, int index)
        {
            if (candles == null || index < _trailCandlesLong.ValueInt || index < _trailCandlesShort.ValueInt)
            {
                return 0;
            }

            if (side == Side.Buy)
            {
                decimal price = decimal.MaxValue;

                for (int i = index; i > index - _trailCandlesLong.ValueInt; i--)
                {
                    if (candles[i].Low < price)
                    {
                        price = candles[i].Low;
                    }
                }
                return price;
            }

            if (side == Side.Sell)
            {
                decimal price = 0;

                for (int i = index; i > index - _trailCandlesShort.ValueInt; i--)
                {
                    if (candles[i].High > price)
                    {
                        price = candles[i].High;
                    }
                }

                return price;
            }
            return 0;
        }

        // Method for calculating the volume of entry into a position
        private decimal GetVolume(BotTabSimple tab)
        {
            decimal volume = 0;

            if (_volumeType.ValueString == "Contracts")
            {
                volume = _volume.ValueDecimal;
            }
            else if (_volumeType.ValueString == "Contract currency")
            {
                decimal contractPrice = tab.PriceBestAsk;
                volume = _volume.ValueDecimal / contractPrice;

                if (StartProgram == StartProgram.IsOsTrader)
                {
                    IServerPermission serverPermission = ServerMaster.GetServerPermission(tab.Connector.ServerType);

                    if (serverPermission != null &&
                        serverPermission.IsUseLotToCalculateProfit &&
                    tab.Security.Lot != 0 &&
                        tab.Security.Lot > 1)
                    {
                        volume = _volume.ValueDecimal / (contractPrice * tab.Security.Lot);
                    }

                    volume = Math.Round(volume, tab.Security.DecimalsVolume);
                }
                else // Tester or Optimizer
                {
                    volume = Math.Round(volume, 6);
                }
            }
            else if (_volumeType.ValueString == "Deposit percent")
            {
                Portfolio myPortfolio = tab.Portfolio;

                if (myPortfolio == null)
                {
                    return 0;
                }

                decimal portfolioPrimeAsset = 0;

                if (_tradeAssetInPortfolio.ValueString == "Prime")
                {
                    portfolioPrimeAsset = myPortfolio.ValueCurrent;
                }
                else
                {
                    List<PositionOnBoard> positionOnBoard = myPortfolio.GetPositionOnBoard();

                    if (positionOnBoard == null)
                    {
                        return 0;
                    }

                    for (int i = 0; i < positionOnBoard.Count; i++)
                    {
                        if (positionOnBoard[i].SecurityNameCode == _tradeAssetInPortfolio.ValueString)
                        {
                            portfolioPrimeAsset = positionOnBoard[i].ValueCurrent;
                            break;
                        }
                    }
                }

                if (portfolioPrimeAsset == 0)
                {
                    SendNewLogMessage("Can`t found portfolio " + _tradeAssetInPortfolio.ValueString, Logging.LogMessageType.Error);
                    return 0;
                }

                decimal moneyOnPosition = portfolioPrimeAsset * (_volume.ValueDecimal / 100);

                decimal qty = moneyOnPosition / tab.PriceBestAsk / tab.Security.Lot;

                if (tab.StartProgram == StartProgram.IsOsTrader)
                {
                    qty = Math.Round(qty, tab.Security.DecimalsVolume);
                }
                else
                {
                    qty = Math.Round(qty, 7);
                }

                return qty;
            }

            return volume;
        }
    }
}