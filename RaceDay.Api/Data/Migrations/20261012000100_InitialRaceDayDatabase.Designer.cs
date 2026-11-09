using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RaceDay.Api.Data;

namespace RaceDay.Api.Data.Migrations;

[DbContext(typeof(RaceDayDbContext))]
[Migration("20261012000100_InitialRaceDayDatabase")]
partial class InitialRaceDayDatabase
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder) => RaceDayDbContextModelSnapshot.ConfigureModel(modelBuilder);
}
