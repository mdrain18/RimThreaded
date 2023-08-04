# RimThreaded

RimThreaded enables RimWorld to utilize multiple threads and thus greatly increases the speed of the game.

JOIN OUR COMMUNITY ON DISCORD:  
https://discord.gg/3JJuWK8

WIKI (including MOD COMPATIBILITY):
https://github.com/cseelhoff/RimThreaded/wiki

MOD COMPATIBILITY:
https://github.com/cseelhoff/RimThreaded/wiki/Mod-Compatibility

SETTINGS:
The number of threads to utilize should be set in the mod settings, according to your specific computer's core count.

LOAD ORDER:  
Put RimThreaded last in load order.

SUBMIT BUGS:  
https://github.com/cseelhoff/RimThreaded/issues/new/choose

CREDITS:  
Big thanks to Sernior for his continued help bug fixing and performance tweaks!  
Big thanks to JoJo for his continued help bug fixing and adding mod compatibility!  
Big thanks to Brrainz (Pardeike) for Harmony and all of the coding help!
Big thanks to Kiame Vivacity for his help with fixing sound!  
Special thank you for helping me test Austin (Stanui)!  
Thank you bookdude13 for your many bugfixes!  
Thank you to Ataman for helping me fix the LVM deep storage bug!  
Thank you Ken for fixing RegionCostCalculator.PathableNeighborIndices!  
Thank you Raccoononi for the RT 2.0 logo! https://discordhub.com/profile/245738467995156481  
Thank you ArchieV1 for the RT 1.0 logo! https://github.com/ArchieV1  
Logo help from: Marnador https://ludeon.com/forums/index.php?action=profile;u=36313 and
JKimsey https://pixabay.com/users/jkimsey-253161/  
Thank you BaRKy for reviewing my mod! I am honored! https://www.youtube.com/watch?v=EWudgTJksMU  
And thank you to others in Rimworld community who have posted their bug findings!

DONATE:  
Some subscribers insisted that I set up a donation page. For those looking, here it is: https://ko-fi.com/rimthreaded

CHANGE LOG:

Version 2.7.2  
-Added RimWorld 1.4.8333.294475 support  
-Fixed some bugs with Remove Hediffs including pregnancy bug  
-Fixed alerts not appearing bug

Version 2.7.1  
-Added RimWorld 1.4.8320.21511 (12-OCT) support  
-Fixed some bugs with HediffSet

Version 2.7.0  
-Added RimWorld 1.4 support  
-Optimized Memory Usage

Version 2.6.4  
-Fixed a bug with GiddyUp  
-Optimized Memory Usage

Version 2.6.3  
-Fixed another reservation bug  
-Fixed a bug with Social Opinions  
-Added caching for common thing request groups

Version 2.6.2  
-Fixed Reservation Bug

Version 2.6.0 - For Leo.S  
-Recompiled for RimWorld 1.3.3200  
-Removed support for RimWorld version 1.2  
-Added SpeakUp Compatibility

Version 2.5.15 - Sith Memory Rub   
-Fixed bug in RecordWorker_TimeGettingJoy  
-Fixed bug in HediffSet.AddDirect  
-Fixed bug in MemoryThoughtHandler.TryGainMemory  
-Fixed bug in Pawn_HealthTracker.CheckForStateChange  
-Fixed bug in Pawn_HealthTracker.PostApplyDamage  
-Fixed bug in SituationalThoughtHandler.RemoveExpiredThoughtsFromCache  
-Lowered Transpile Harmony priority for RT methods  
-Removed disablelimits in RT settings  
-Transpiled Thing.TakeDamage

Version 2.5.14 - First Intergalactic War  
-Added better ReplaceStuff Compatibility  
-Added better RimWar Compatibility  
-Added better JobsOfOppurtunity Compatibility  
-Fixed bug in Hauling  
-Fixed bug in RimWorld.Planet.TileFinder  
-Fixed bugs in Verse.PawnTextureAtlas  
-Fixed bug in Pawn_RotationTracker.UpdateRotation  
-Fixed ReplacementField Assembly Caches doubling work

Version 2.5.13 - Jedi Temple Doors  
-Doors Expanded now compatible  
-ThreadSafeLinkedList decoupling and added to nuget  
-Fixed VEE Bug with GraphicRequest.Get

Version 2.5.12 - Obsolete Droids  
-Fixed bug when using Android Tiers with RW 1.2  
-Added Vanilla Events Extended Compatibility  
-Added thread-safety for CombatExtended.Utilities.ThingsTrackingModel  
-Fixed A psychic droner tuned to the None gender is driving Nones mad #434  
-Removed outdated mod compatibility patches  
-Added GiddyUp Compatibility

Version 2.5.11 - Uparmored Jawa Sandcrawler  
-Fixed GenCollection.RemoveAll bug  
-Fixed Roof Notification bug  
-Fixed Shuttle bug

Version 2.5.10 - Jawa Sandcrawler  
-Fixed Hydroponics Bug  
-Optimized Planting Caches and Sowing Caches  
-Fixed Hauling Priorities bug  
-Fixed bug with caravans and riders leaving the map  
-Fixed bug when pawns not taking damage prior to being placed on map  
-Added compatibility for new AndroidTiers mod  
-Fixed misc Royalty bugs

Version 2.5.9 - The Reformed Jedi Counsel  
-Reogranized folder structure of simplify builds and improve cooperative coding efforts

Version 2.5.8 - Vader's Imperial Shuttle  
-Fixed bug with shuttles not taking off  
-Fixed bug with roofed rooms showing as unroofed

Version 2.5.7 - Clear, your mind must be  
-Fixed bug in RemoveAll_Pawn_CachedSocialThoughts  
-Fixed bug in StatWorker.get_Worker  
-Fixed bug when raiders appear

Version 2.5.6 - Free Wookie Hugs  
-RimWorld 1.3 Compatibility added  
-Optimized PhysicalInteractionReservationManager_Patch  
-Fixed many null reference bugs found during explosions

Version 2.4.3 - A long long time ago, in a galaxy with working plant harvesting...  
-Fixed another bug with colonists not harvesting mature crops

Version 2.4.2 - The Lost Ewok Farmer  
-Fixed bug with Trees not spawning  
-Fixed bug with colonists not harvesting mature crops

Version 2.4.1 - Tatooine Moisture Farm  
-Optimized Performance for Harvesting Job  
-Fixed Caravan not being able to leave map bug #564  
-Fixed issue with multiple trade ships arriving concurrently

Version 2.3.9 - Episode V - The Threads Strike Back  
-New RimThreaded 2.0 was rewritten from the ground up  
-Major performance increases. Some saved games with 700+ pawns went from 1-2TPS to 20-40TPS  
-Major mod compatibility improvements. Over 1000 mods have been tested and are now compatible, including CE, SOS2, and
ZombieLand!  
-Only 7 mods remain as known incompatible. https://github.com/cseelhoff/RimThreaded/wiki/Mod-Compatibility  
-New logo! Thanks Raccoononi!  
