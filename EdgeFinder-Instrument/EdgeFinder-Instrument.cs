using System;
using System.Linq;
using System;
using System.Linq;
using System;
using System.Linq;
using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;

namespace cAlgo.Robots
{
    public enum StrategyType
    {
        Breakout,
        BreakoutExtended,
        MeanReversion,
        MeanReversionExtended,
        BreakoutRsi,
        MeanReversionRsi
    }

    public enum TradeDirection
    {
        LongOnly,
        ShortOnly,
        Both
    }

    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class EdgeFinderInstrument : Robot
    {
        private RelativeStrengthIndex _rsi;

        [Parameter("Strategy", DefaultValue = StrategyType.Breakout)]
        public StrategyType Strategy { get; set; }

        [Parameter("Direction", DefaultValue = TradeDirection.Both)]
        public TradeDirection Direction { get; set; }

        [Parameter("Volume (Lots)", DefaultValue = 1.0)]
        public double VolumeInLots { get; set; }

        [Parameter("Label", DefaultValue = "EdgeFinder")]
        public string Label { get; set; }

        [Parameter("RSI Period", DefaultValue = 2)]
        public int RsiPeriod { get; set; }

        protected override void OnStart()
        {
            Positions.Opened += OnPositionOpened;
            _rsi = Indicators.RelativeStrengthIndex(Bars.ClosePrices, RsiPeriod);
        }

        protected override void OnBar()
        {
            switch (Strategy)
            {
                case StrategyType.Breakout:
                    RunBreakoutStrategy();
                    break;
                case StrategyType.BreakoutExtended:
                    RunBreakoutExtendedStrategy();
                    break;
                case StrategyType.MeanReversion:
                    RunMeanReversionStrategy();
                    break;
                case StrategyType.MeanReversionExtended:
                    RunMeanReversionExtendedStrategy();
                    break;
                case StrategyType.BreakoutRsi:
                    RunBreakoutRsiStrategy();
                    break;
                case StrategyType.MeanReversionRsi:
                    RunMeanReversionRsiStrategy();
                    break;
            }
        }

        private void RunBreakoutStrategy()
        {
            CancelPendingOrders();

            // Calculate High and Low of the previous bar
            // Index 1 is the last closed bar
            double high = Bars.HighPrices.Last(1);
            double low = Bars.LowPrices.Last(1);

            // Calculate volume
            double volume = Symbol.QuantityToVolumeInUnits(VolumeInLots);

            // Check for existing open positions
            var positions = Positions.FindAll(Label, SymbolName);
            bool hasBuyPosition = positions.Any(p => p.TradeType == TradeType.Buy);
            bool hasSellPosition = positions.Any(p => p.TradeType == TradeType.Sell);

            // Place Buy Stop at High if no Sell position exists (or no positions at all)
            // Logic: If we have a Buy position, we DO NOT place a Buy Stop. We only place a Sell Stop.
            // If we have a Sell position, we DO NOT place a Sell Stop. We only place a Buy Stop.
            
            // Check Direction constraints
            bool canBuy = Direction == TradeDirection.Both || Direction == TradeDirection.LongOnly;
            bool canSell = Direction == TradeDirection.Both || Direction == TradeDirection.ShortOnly;

            if (canBuy && !hasBuyPosition)
            {
                PlaceStopOrder(TradeType.Buy, SymbolName, volume, high, Label);
            }

            if (canSell && !hasSellPosition)
            {
                PlaceStopOrder(TradeType.Sell, SymbolName, volume, low, Label);
            }
        }

        private void RunBreakoutExtendedStrategy()
        {
            CancelPendingOrders();

            // Index 1 is the last closed bar
            double high = Bars.HighPrices.Last(1);
            double low = Bars.LowPrices.Last(1);
            double close = Bars.ClosePrices.Last(1);
            double open = Bars.OpenPrices.Last(1);
            double previousClose = Bars.ClosePrices.Last(2);

            // Calculate volume
            double volume = Symbol.QuantityToVolumeInUnits(VolumeInLots);

            // Check for existing open positions
            var positions = Positions.FindAll(Label, SymbolName);
            bool hasBuyPosition = positions.Any(p => p.TradeType == TradeType.Buy);
            bool hasSellPosition = positions.Any(p => p.TradeType == TradeType.Sell);

            // Check Direction constraints
            bool canBuy = Direction == TradeDirection.Both || Direction == TradeDirection.LongOnly;
            bool canSell = Direction == TradeDirection.Both || Direction == TradeDirection.ShortOnly;

            // Buy Condition: Close > Previous Close AND Close > Open
            if (canBuy && !hasBuyPosition && close > previousClose && close > open)
            {
                PlaceStopOrder(TradeType.Buy, SymbolName, volume, high, Label);
            }

            // Sell Condition: Close < Previous Close AND Close < Open
            if (canSell && !hasSellPosition && close < previousClose && close < open)
            {
                PlaceStopOrder(TradeType.Sell, SymbolName, volume, low, Label);
            }
        }

        private void RunMeanReversionStrategy()
        {
            CancelPendingOrders();

            // Index 1 is the last closed bar
            double high = Bars.HighPrices.Last(1);
            double low = Bars.LowPrices.Last(1);

            // Calculate volume
            double volume = Symbol.QuantityToVolumeInUnits(VolumeInLots);

            // Check for existing open positions
            var positions = Positions.FindAll(Label, SymbolName);
            bool hasBuyPosition = positions.Any(p => p.TradeType == TradeType.Buy);
            bool hasSellPosition = positions.Any(p => p.TradeType == TradeType.Sell);

            // Check Direction constraints
            bool canBuy = Direction == TradeDirection.Both || Direction == TradeDirection.LongOnly;
            bool canSell = Direction == TradeDirection.Both || Direction == TradeDirection.ShortOnly;

            // Buy Limit at Low
            if (canBuy && !hasBuyPosition)
            {
                PlaceLimitOrder(TradeType.Buy, SymbolName, volume, low, Label);
            }

            // Sell Limit at High
            if (canSell && !hasSellPosition)
            {
                PlaceLimitOrder(TradeType.Sell, SymbolName, volume, high, Label);
            }
        }

        private void RunMeanReversionExtendedStrategy()
        {
            CancelPendingOrders();

            // Index 1 is the last closed bar
            double high = Bars.HighPrices.Last(1);
            double low = Bars.LowPrices.Last(1);
            double close = Bars.ClosePrices.Last(1);
            double open = Bars.OpenPrices.Last(1);
            double previousClose = Bars.ClosePrices.Last(2);

            // Calculate volume
            double volume = Symbol.QuantityToVolumeInUnits(VolumeInLots);

            // Check for existing open positions
            var positions = Positions.FindAll(Label, SymbolName);
            bool hasBuyPosition = positions.Any(p => p.TradeType == TradeType.Buy);
            bool hasSellPosition = positions.Any(p => p.TradeType == TradeType.Sell);

            // Check Direction constraints
            bool canBuy = Direction == TradeDirection.Both || Direction == TradeDirection.LongOnly;
            bool canSell = Direction == TradeDirection.Both || Direction == TradeDirection.ShortOnly;

            // Buy Condition: Close < Previous Close AND Close < Open -> Buy Limit at Low
            if (canBuy && !hasBuyPosition && close < previousClose && close < open)
            {
                PlaceLimitOrder(TradeType.Buy, SymbolName, volume, low, Label);
            }

            // Sell Condition: Close > Previous Close AND Close > Open -> Sell Limit at High
            if (canSell && !hasSellPosition && close > previousClose && close > open)
            {
                PlaceLimitOrder(TradeType.Sell, SymbolName, volume, high, Label);
            }
        }

        private void RunBreakoutRsiStrategy()
        {
            CancelPendingOrders();

            double rsiValue = _rsi.Result.Last(1);
            double volume = Symbol.QuantityToVolumeInUnits(VolumeInLots);

            var positions = Positions.FindAll(Label, SymbolName);
            bool hasBuyPosition = positions.Any(p => p.TradeType == TradeType.Buy);
            bool hasSellPosition = positions.Any(p => p.TradeType == TradeType.Sell);

            bool canBuy = Direction == TradeDirection.Both || Direction == TradeDirection.LongOnly;
            bool canSell = Direction == TradeDirection.Both || Direction == TradeDirection.ShortOnly;

            // Buy if RSI > 75
            if (canBuy && !hasBuyPosition && rsiValue > 75)
            {
                ExecuteMarketOrder(TradeType.Buy, SymbolName, volume, Label);
            }

            // Sell if RSI < 25
            if (canSell && !hasSellPosition && rsiValue < 25)
            {
                ExecuteMarketOrder(TradeType.Sell, SymbolName, volume, Label);
            }
        }

        private void RunMeanReversionRsiStrategy()
        {
            CancelPendingOrders();

            double rsiValue = _rsi.Result.Last(1);
            double volume = Symbol.QuantityToVolumeInUnits(VolumeInLots);

            var positions = Positions.FindAll(Label, SymbolName);
            bool hasBuyPosition = positions.Any(p => p.TradeType == TradeType.Buy);
            bool hasSellPosition = positions.Any(p => p.TradeType == TradeType.Sell);

            bool canBuy = Direction == TradeDirection.Both || Direction == TradeDirection.LongOnly;
            bool canSell = Direction == TradeDirection.Both || Direction == TradeDirection.ShortOnly;

            // Buy if RSI < 25
            if (canBuy && !hasBuyPosition && rsiValue < 25)
            {
                ExecuteMarketOrder(TradeType.Buy, SymbolName, volume, Label);
            }

            // Sell if RSI > 75
            if (canSell && !hasSellPosition && rsiValue > 75)
            {
                ExecuteMarketOrder(TradeType.Sell, SymbolName, volume, Label);
            }
        }

        private void CancelPendingOrders()
        {
            foreach (var order in PendingOrders)
            {
                if (order.Label == Label)
                {
                    CancelPendingOrder(order);
                }
            }
        }

        private void OnPositionOpened(PositionOpenedEventArgs args)
        {
            var position = args.Position;

            // Only manage positions opened by this bot
            if (position.Label != Label) return;

            // Close opposite positions
            var oppositeType = position.TradeType == TradeType.Buy ? TradeType.Sell : TradeType.Buy;
            var oppositePositions = Positions.FindAll(Label, SymbolName, oppositeType);

            foreach (var oppositePosition in oppositePositions)
            {
                ClosePosition(oppositePosition);
            }
        }

        protected override void OnStop()
        {
            // Close all open positions created by this bot
            foreach (var position in Positions.FindAll(Label, SymbolName))
            {
                ClosePosition(position);
            }

            CancelPendingOrders();
        }
    }
}