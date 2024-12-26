# pendler-statistik

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

## Build

```
dotnet publish -c Release --self-contained -r win-x86 -o release
```
