/*
   Bitvantage.IPNetworkExtensions
   Copyright (C) 2024 Michael Crino
   
   This program is free software: you can redistribute it and/or modify
   it under the terms of the GNU Affero General Public License as published by
   the Free Software Foundation, either version 3 of the License, or
   (at your option) any later version.
   
   This program is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU Affero General Public License for more details.
   
   You should have received a copy of the GNU Affero General Public License
   along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using BenchmarkDotNet.Running;

// runs the benchmarks chosen on the command line, or asks which to run when there are none; for example:
//   dotnet run -c Release --project Benchmarks -- --filter '*'
//   dotnet run -c Release --project Benchmarks -- --filter '*Containment*' --job short
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
