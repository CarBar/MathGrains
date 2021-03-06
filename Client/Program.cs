﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Orleans.Configuration;
using GrainInterfaces;

namespace OrleansClient
{
    /// <summary>
    /// Orleans test silo client
    /// </summary>
    public class Program
    {
        static int Main(string[] args)
        {
            return RunMainAsync().Result;
        }

        private static async Task<int> RunMainAsync()
        {
            try
            {
                using (var client = await StartClientWithRetries())
                {
                    await DoClientWork(client);
                    Console.ReadKey();
                }

                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadKey();
                return 1;
            }
        }

        private static async Task<IClusterClient> StartClientWithRetries(int initializeAttemptsBeforeFailing = 5)
        {
            int attempt = 0;
            IClusterClient client;
            while (true)
            {
                try
                {
                    client = new ClientBuilder()
                        .UseLocalhostClustering()
                        .Configure<ClusterOptions>(options =>
                        {
                            options.ClusterId = "dev";
                            options.ServiceId = "HelloWorldApp";
                        })
                        .ConfigureLogging(logging => logging.AddConsole())
                        .Build();

                    await client.Connect();
                    Console.WriteLine("Client successfully connected to silo host");
                    break;
                }
                catch (SiloUnavailableException)
                {
                    attempt++;
                    Console.WriteLine($"Attempt {attempt} of {initializeAttemptsBeforeFailing} failed to initialize the Orleans client.");
                    if (attempt > initializeAttemptsBeforeFailing)
                    {
                        throw;
                    }
                    await Task.Delay(TimeSpan.FromSeconds(4));
                }
            }

            return client;
        }

        private static async Task DoClientWork(IClusterClient client)
        {
            // example of calling grains from the initialized client
            ISquareGrain mySquareGrain = client.GetGrain<ISquareGrain>(Guid.NewGuid());
            ICubeGrain myCubeGrain = client.GetGrain<ICubeGrain>(Guid.NewGuid());

            Console.WriteLine("\n\nEnter a number:");
            decimal mathMe = 0;

            while (!decimal.TryParse(Console.ReadLine(), out mathMe))
            {
                Console.WriteLine("That was not a valid entry.");
                Console.WriteLine("Try again:");
            }

            decimal squaredResult = await mySquareGrain.SquareMe(mathMe);
            decimal cubedResult = await myCubeGrain.CubeMe(mathMe);
            Console.WriteLine("\n The square of {0} is {1}.\n\n", mathMe, squaredResult);
            Console.WriteLine("\n The cube of {0} is {1}. \n\n", mathMe, cubedResult);
        }
    }
}
