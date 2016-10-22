﻿using BasketGame.Core.GuessStrategies;
using BasketGame.Core.Players;
using System;
using System.Collections.Generic;
using System.Threading;

namespace BasketGame.Core
{
    public class Game
    {
        private List<GenericPlayer<IGuessStrategy>> _players;
        private Basket _basket;

        private ManualResetEvent _finilizeEvent;
        private Thread _finilizerThread;

        public GenericPlayer<IGuessStrategy> Winner { get; private set; }
        public GameRestriction restr;

        private object _syncObject = new object();

        public void Initialize(List<Input> inputs, int min, int max)
        {
            _basket = new Basket(min, max);
            restr = new GameRestriction(min, max);

            _players = new List<GenericPlayer<IGuessStrategy>>();
            
            //GeneralPlayer.OnNumberGueesed += OnNumberGuessedHandler;
            _finilizerThread = new Thread(FinilizeProc);
            _finilizeEvent = new ManualResetEvent(false);
            _finilizerThread.Start();

            foreach (var input in inputs)
            {
                var newPlayer = PlayersFactory.GetPlayer(input.Name, input.PlayerType);
                _players.Add(newPlayer);
                newPlayer.OnNumberGueesed += OnNumberGuessedHandler;

            }

        }

        public void Play()
        {
            foreach (var player in _players)
                player.Start(restr);
        }

        public void OnNumberGuessedHandler(object sender, PlayerNumberEventArgs args)
        {
            var player = sender as GenericPlayer<IGuessStrategy>;
            lock (_syncObject)
            {
                if (Winner == null)
                {
                    if (args.GuessedNumber == _basket.Weight)
                    {
                        Winner = player;
                        Console.WriteLine(player.Name + " won!");
                        Console.WriteLine(_basket.Weight);
                        _finilizeEvent.Set();
                    }
                    else
                    {
                        Console.WriteLine(String.Format("{0}: {1}", args.PlayerName, args.GuessedNumber));
                    }
                }
            }

            var delta = Math.Abs(_basket.Weight - args.GuessedNumber);
            player.Wait(delta);
        }

        public void FinilizeProc()
        {
            _finilizeEvent.WaitOne();

            foreach (var player in _players)
                player.Abort();
        }
    }
}