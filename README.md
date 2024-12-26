# pendler-statistik

Simple Danish train commuter stats service, running e.g. at http://pendler-statistik.selsmark.dk/query/tracks/HH

Based on https://mittog.dk API

## Setup and run locally

```
cd TrainStats.Service
docker run -e MYSQL_ROOT_PASSWORD=password -p 3306:3306 mysql
```

Connect to SQL server and create database using
```
CREATE DATABASE trains
```

Run service
```
dotnet watch
```

Create database table by opening http://localhost:5231/install

Then open e.g. http://localhost:5231/fetch/HH to populate table and http://localhost:5231/query/tracks/HH to see stats (dpending on MySQL setup, there might be difference in timezone)

## How to use

System is designed to be accessed and managed through browser, using the urls specified here.

### `/about`

Show some system details for site, e.g. next train for station(s).

### `/fetch/{station}`

Fetches train data for the specified station and stores in database. Returns either `-1` if no data needed to be updated (no trains since last fetch, for optimization), or number of rows updated.

Configure a cron job to call this endpoint, e.g. every minute, for stations to collect statistics for. If no need to fetch data, it will simply exit, so no risk of overloading the backend API or database.

### `/install`

Creates necessary table in database. Will simply fail if already exists.

### `/query/delays/{station}`

Shows simple stats page of trains delayed more than 2 mins for specified station.

### `/query/cancellations/{station}`

Shows cancelled trains for station.

### `/query/tracks/{station}`

Shows which tracks are in use for stations

### `/query/tracks/detailed/{station}`

More detailed information about tracks, grouped by destination station.

### `/trains/{station}`

Mostly for development purposes. Calls the backend and loads train information.


## Build and deploy

```
dotnet publish -c Release --self-contained -r win-x86 -o release
```

Which can then be deployed to your ASP.NET based site. If you experience existing files being locked, rename `web.config` to temporarily shut down website.
