﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;

namespace Parking
{
    static class Settings
    {
        private static Timer aTimer,bTimer;
        static object locker = new object();
        static object locker1 = new object();

        public static readonly int parkingSpaceLimit = 20;
        private static int parkingSpace = parkingSpaceLimit;
        private static readonly string LOGPATH = "Transaction.log";
        private static double fine = 1.2;

        public static int ParkingSpace { get { return parkingSpace; } private set { parkingSpace = value; } }       
        public static double Fine { get { return fine; } private set { fine = value; } }
        
        public static readonly Dictionary<string, int> carPrices 
            = new Dictionary<string, int>
            {   
                { "Truck", 5 },
                { "Passenger", 3},
                { "Bus", 2},
                { "Motorcycle", 1}
            };

        public static int TimeOut
        {
            set
            {
                aTimer = new Timer(3000);
                aTimer.Elapsed += new ElapsedEventHandler(Count);
                aTimer.Enabled = true;
            }
        }
        public static int LogTime
        {
            set
            {
                bTimer = new Timer(60000);
                bTimer.Elapsed += new ElapsedEventHandler(LogWriter);
                bTimer.Enabled = true;
            }
        }

        private static void Count(object obj,EventArgs e)
        {
            lock (locker)
            {
                double money = 0;
                foreach (Car car in Parking.Cars.ToArray())
                {
                    foreach (KeyValuePair<string, int> entry in carPrices)
                    {
                        String sCarType = Convert.ToString(car.Type);
                        if (sCarType == entry.Key)
                        {
                            if (car.Balance < entry.Value)
                                money = Fine * carPrices[sCarType];
                            else money = entry.Value;

                            Parking.CurrentBalance += money;
                            Parking.Balance += money;
                            car.Balance -= money;

                            Parking.Transactions.Add(new Transaction(car.Id, DateTime.Now, money));

                        }
                    }
                }
            }
        }

        public static void LogWriter(object obj, EventArgs e)
        {
            lock (locker1)
            {
                DateTime currentTime = DateTime.Now;

                using (StreamWriter w = File.AppendText(LOGPATH))
                {
                    foreach (Transaction tr in Parking.Transactions.ToArray())
                    {
                        TimeSpan tsp = tr.DateTimeTransaction - currentTime;
                        if (tsp.TotalMinutes <= 1) {
                            Parking.Balance += tr.WrittenOff_Funds;
                        }
                    }
                    w.WriteLine("Current time:{0} Transaction sum:{1}", DateTime.Now, Parking.Balance);
                    Parking.Transactions.Clear();
                    Parking.CurrentBalance = 0;
                }
                
            }
            
        }
        public static void LogReader()
        {       
            lock (locker1)
            {
                if (!File.Exists(LOGPATH))
                {
                    Console.WriteLine("Sorry... transaction log file not created yet. ");
                    return;
                }
                using (StreamReader r = File.OpenText(LOGPATH))
                {
                    DumpLog(r);
                }
            }
        }
   
        public static void DumpLog(StreamReader r)
        {
            string line;
            while ((line = r.ReadLine()) != null)
            {
                Console.WriteLine(line);
            }
        }
    }
}