﻿/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.Market.Servers;
using OsEngine.Market;
using OsEngine.Language;

/* Description
trading robot for osengine

The trend robot on intersection of SMA with OffsetSma

Buy: Fast Sma is higher than slow Sma

Sell: Fast Sma below slow Sma

Exit: on the opposite signal
*/

namespace OsEngine.Robots
{
    [Bot("SmaWithAShift")] // Instead of manually adding through BotFactory, we use an attribute to simplify the process.
    public class SmaWithAShift : BotPanel
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

        // Indicator settings
        private StrategyParameterInt _periodSmaFast;
        private StrategyParameterInt _periodOsmaSlow;
        private StrategyParameterInt _offset;

        // Indicator
        private Aindicator _smaFast;
        private Aindicator _osmaSlow;

        // The last value of the indicators
        private decimal _lastSmaFast;
        private decimal _lastOsmaSlow;

        public SmaWithAShift(string name, StartProgram startProgram) : base(name, startProgram)
        {
            // Create and assign the main trading tab
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            // Basic settings
            _regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" }, "Base");
            _slippage = CreateParameter("Slippage %", 0m, 0, 20, 1, "Base");
            _startTradeTime = CreateParameterTimeOfDay("Start Trade Time", 0, 0, 0, 0, "Base");
            _endTradeTime = CreateParameterTimeOfDay("End Trade Time", 24, 0, 0, 0, "Base");

            // GetVolume Settings
            _volumeType = CreateParameter("Volume type", "Deposit percent", new[] { "Contracts", "Contract currency", "Deposit percent" });
            _volume = CreateParameter("Volume", 20, 1.0m, 50, 4);
            _tradeAssetInPortfolio = CreateParameter("Asset in portfolio", "Prime");

            // Indicator settings
            _periodSmaFast = CreateParameter("Period SMA Fast", 100, 10, 300, 10, "Indicator");
            _periodOsmaSlow = CreateParameter("Period SMA Slow", 200, 10, 300, 10, "Indicator");
            _offset = CreateParameter("Offset", 3, 1, 20, 1, "Indicator");

            // Create indicator SmaFast
            _smaFast = IndicatorsFactory.CreateIndicatorByName("Sma", name + "SmaFast", false);
            _smaFast = (Aindicator)_tab.CreateCandleIndicator(_smaFast, "Prime");
            ((IndicatorParameterInt)_smaFast.Parameters[0]).ValueInt = _periodSmaFast.ValueInt;
            _smaFast.Save();

            // Create indicator OffsetSmaSlow
            _osmaSlow = IndicatorsFactory.CreateIndicatorByName("OffsetSma", name + "OsmaSlow", false);
            _osmaSlow = (Aindicator)_tab.CreateCandleIndicator(_osmaSlow, "Prime");
            _osmaSlow.DataSeries[0].Color = System.Drawing.Color.Yellow;
            ((IndicatorParameterInt)_osmaSlow.Parameters[0]).ValueInt = _periodOsmaSlow.ValueInt;
            ((IndicatorParameterInt)_osmaSlow.Parameters[1]).ValueInt = _offset.ValueInt;
            _osmaSlow.Save();

            // Subscribe to the indicator update event
            ParametrsChangeByUser += SmaWithAShift_ParametrsChangeByUser;

            // Subscribe to the candle finished event
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;

            Description = OsLocalization.Description.DescriptionLabel235;
        }
        
        // Indicator Update event
        private void SmaWithAShift_ParametrsChangeByUser()
        {
            ((IndicatorParameterInt)_smaFast.Parameters[0]).ValueInt = _periodSmaFast.ValueInt;
            _smaFast.Save();
            _smaFast.Reload();

            ((IndicatorParameterInt)_osmaSlow.Parameters[0]).ValueInt = _periodOsmaSlow.ValueInt;
            ((IndicatorParameterInt)_osmaSlow.Parameters[1]).ValueInt = _offset.ValueInt;
            _osmaSlow.Save();
            _osmaSlow.Reload();
        }

        public override string GetNameStrategyType()
        {
            return "SmaWithAShift";
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

            // If there are not enough candles to build an indicator, we exit
            if (candles.Count <= _periodSmaFast.ValueInt || candles.Count <= _periodOsmaSlow.ValueInt + 3)
            {
                return;
            }
            SendNewLogMessage($"{_smaFast.DataSeries[0].Last} {_osmaSlow.DataSeries[0].Last}", Logging.LogMessageType.User);
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
            List<Position> openPositions = _tab.PositionsOpenAll;

            if (openPositions == null || openPositions.Count == 0)
            {
                // The last value of the indicators
                _lastSmaFast = _smaFast.DataSeries[0].Last;
                _lastOsmaSlow = _osmaSlow.DataSeries[0].Last;

                decimal _slippage = this._slippage.ValueDecimal * _tab.Securiti.PriceStep;

                // Long
                if (_regime.ValueString != "OnlyShort") // If the mode is not only short, then we enter long
                {
                    if (_lastSmaFast > _lastOsmaSlow)
                    {
                        _tab.BuyAtLimit(GetVolume(_tab), _tab.PriceBestAsk + _slippage);
                    }
                }

                // Short
                if (_regime.ValueString != "OnlyLong") // If the mode is not only long, then we enter short
                {
                    if (_lastSmaFast < _lastOsmaSlow)
                    {
                        _tab.SellAtLimit(GetVolume(_tab), _tab.PriceBestAsk - _slippage);
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
                if (openPositions[i].State != PositionStateType.Open)
                {
                    continue;
                }

                // The last value of the indicators
                _lastSmaFast = _smaFast.DataSeries[0].Last;
                _lastOsmaSlow = _osmaSlow.DataSeries[0].Last;

                if (openPositions[i].Direction == Side.Buy) // If the direction of the position is long
                {
                    if (_lastSmaFast < _lastOsmaSlow)
                    {
                        decimal lastPrice = candles[candles.Count - 1].Close;
                        _tab.CloseAtLimit(openPositions[i], lastPrice - _slippage, openPositions[i].OpenVolume);
                    }
                }
                else // If the direction of the position is short
                {
                    if (_lastSmaFast > _lastOsmaSlow)
                    {
                        decimal lastPrice = candles[candles.Count - 1].Close;
                        _tab.CloseAtLimit(openPositions[i], lastPrice + _slippage, openPositions[i].OpenVolume);
                    }
                }
            }
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