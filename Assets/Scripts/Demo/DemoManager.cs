using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CodingDaniel.MapEditor.MEEditor.MESave;
using Demo.Entity;
using Demo.Entity.Enemy;
using Demo.UI;
using Extensions;
using Manager;
using Map;
using Menu;
using Multiplayer;
using Multiplayer.Entity.Client;
using Multiplayer.Entity.Client.Enemy;
using Multiplayer.Entity.Client.Enemy.Animation;
using Multiplayer.Entity.Client.Enemy.State;
using Multiplayer.Entity.Server.Enemy;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Save;
using Steamworks;
using UnityEngine;
using Utils;
using Weapon;

namespace Demo
{
    public class DemoManager : MonoBehaviour
    {
        public static DemoManager Instance { get;private set; }

        public static FileInfo[] DemoFiles = Array.Empty<FileInfo>();
        private static DemoData RecordDemo { get; set; }
        private static DemoData ReplayDemo { get; set; }

        public static Action<bool> OnDemoLoad; //true -- start, false -- loaded

        public static bool PreciseDemo = true;

        public static Action<bool> OnRecordingChanged;

        private static int PlayerCheckThreshold => 5;

        public static string SavePath { get; set; }

        public static bool Recording = false, Replaying = false;

        public static bool ReplayPaused = false;

        public static int RecordTick = 0, ReplayTick = 0;

        private string _saveName = String.Empty;

        private static Dictionary<int, DemoEntity> Entities { get; } = new();

        private JsonSerializerSettings _jsonSerializerSettings;
        private void Awake()
        {
            Instance = this;

            SavePath = Path.Combine(Application.dataPath, "..", "demos");

            _jsonSerializerSettings = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            
            _jsonSerializerSettings.Converters.Add(new DemoEntityDataConverter());

            if (!Directory.Exists(SavePath))
            {
                Directory.CreateDirectory(SavePath);
            }
            else
            {
                RefreshSaveFile();
            }
        }

        public void RefreshSaveFile()
        {
            string[] files = Directory.GetFiles(SavePath);

            DemoFiles = new FileInfo[files.Length];

            for (int i = 0; i < files.Length; i++)
            {
                DemoFiles[i] = new FileInfo(files[i]);
            }
        }

        private void FixedUpdate()
        {
            DoRecording();
            
            DoReplaying();
        }

        void DoRecording()
        {
            if (Recording)
            {
                AddMovementData();
                
                RecordTick++;
            }
        }

        void DoReplaying(bool forcePlay=false)
        {
            if (Replaying && ReplayDemo.endTick > ReplayTick && (forcePlay || !ReplayPaused))
            {
                CheckEntityData();
                
                DemoCanvas.Instance.SetSliderValue(ReplayTick);
                
                ReplayTick++;
            }
            else if(Replaying)
            {
                foreach (var entity in Entities.Values)
                {
                    if (entity.IsThrowable())
                    {
                        DemoThrowable demoThrowable = (DemoThrowable)entity;
                        
                        demoThrowable.TryToSetLastVelocity();
                    }
                }
            }
        }

        #region Record

        void AddMovementData()
        {
            foreach (var entity in Entities.Values)
            {
                if (RecordDemo.EntityList.TryGetValue(entity.Id, out var entityData))
                {
                    if (entity.IsPlayer())
                    {
                        var player = (DemoPlayer)entity;
                    
                        DemoPlayerData playerData = (DemoPlayerData)entityData;
                        playerData.Positions[RecordTick] = player.GetPosition().ToMyVector3();
                        playerData.Rotations[RecordTick] = player.GetEulerAngleY();
                        playerData.HeadRotations[RecordTick] = player.PlayerAnimation.GetXRotation();
                        playerData.Velocities[RecordTick] = player.playerState.GetVelocity().ToMyVector3();
                    }
                    else if (entity.IsEnemy())
                    {
                        var enemy = (DemoEnemy)entity;

                        DemoEnemyData enemyData = (DemoEnemyData)entityData;

                        enemyData.Positions[RecordTick] = enemy.GetPosition().ToMyVector3();
                        enemyData.Rotations[RecordTick] = enemy.GetEulerAngleY();
                    }
                }
            }
        }

        #region Player

        public void AddPlayerSpawned(ClientPlayer player)
        {
            if (!Recording) return;

            int id = GetNewId();
            
            Vector3 pos = player.demoPlayer.GetPosition();

            int state = player.Dead ? 0 : player.playerState.IsInfected ? 2 : 1;
            
            DemoPlayerData playerData = new DemoPlayerData(id, RecordTick,player.playerState.Username,player.playerState.SteamId,pos, player.playerState.WeaponManager.CurrentWeaponIndex, player.playerState.WeaponManager.WeaponIndexes,  player.playerState.CosmeticIndex, player.playerState.IsInfected)
            {
                Positions =
                {
                    [RecordTick] = pos.ToMyVector3()
                },
                    
                Velocities = 
                {
                    [RecordTick] = player.playerState.GetVelocity().ToMyVector3()
                },
                    
                Rotations =
                {
                    [RecordTick] = player.demoPlayer.GetEulerAngleY()
                },
                    
                HeadRotations =
                {
                    [RecordTick] = player.demoPlayer.GetHeadRotation()
                },
                
                State =
                {
                    [RecordTick] = new DemoPlayerData.StateData(state,-1,0)
                },
                
                Aiming =
                {
                    [RecordTick] = player.playerState.Aiming
                }
            };

            RecordDemo.EntityList.Add(id,playerData);
                
            player.demoPlayer.Spawn(id, player.playerState.Username, player.playerState.SteamId, player.playerState.WeaponManager.CurrentWeaponIndex, player.playerState.WeaponManager.WeaponIndexes, player.playerState.CosmeticIndex, player.playerState.IsInfected);

            Entities[id] = player.demoPlayer;
        }
        public void AddPlayerCrouch(int id, bool crouch)
        {
            if (!Recording) return;
            
            if (RecordDemo.EntityList.TryGetValue(id, out var entityData))
            {
                DemoPlayerData playerData = (DemoPlayerData)entityData;
                playerData.Crouches[RecordTick] = crouch;
            }
        }
        public void AddPlayerAiming(int id, bool aiming)
        {
            if (!Recording) return;
            
            if (RecordDemo.EntityList.TryGetValue(id, out var entityData))
            {
                DemoPlayerData playerData = (DemoPlayerData)entityData;
                playerData.Aiming[RecordTick] = aiming;
            }
        }
        public void AddPlayerWeaponUpdated(int id, int currentWeaponIndex, short[] weapons, ushort[] weaponSkinIndexes)
        {
            if (!Recording) return;
            
            if (RecordDemo.EntityList.TryGetValue(id, out var entityData))
            {
                DemoPlayerData playerData = (DemoPlayerData)entityData;
                playerData.WeaponUpdates[RecordTick] = new DemoPlayerData.WeaponUpdateData(currentWeaponIndex, weapons, weaponSkinIndexes);
            }
        }
        public void AddPlayerWeaponSwitched(int id, int currentWeaponIndex)
        {
            if (!Recording) return;
            
            if (RecordDemo.EntityList.TryGetValue(id, out var entityData))
            {
                DemoPlayerData playerData = (DemoPlayerData)entityData;
                playerData.WeaponSwitched[RecordTick] = currentWeaponIndex;
            }
        }
        public void AddPlayerWeaponShoot(int id, int currentWeaponIndex, Vector3 dir, int bulletCount)
        {
            if (!Recording) return;
            
            if (RecordDemo.EntityList.TryGetValue(id, out var entityData))
            {
                DemoPlayerData playerData = (DemoPlayerData)entityData;
                playerData.WeaponShooting[RecordTick] = new DemoPlayerData.WeaponShootData(currentWeaponIndex, dir, bulletCount);
            }
        }
        public void AddPlayerWeaponReload(int id, int currentWeaponIndex,int bulletCount)
        {
            if (!Recording) return;
            
            if (RecordDemo.EntityList.TryGetValue(id, out var entityData))
            {
                DemoPlayerData playerData = (DemoPlayerData)entityData;
                playerData.WeaponReload[RecordTick] = new DemoPlayerData.WeaponReloadData(currentWeaponIndex, bulletCount);
            }
        }
        
        public void AddPlayerGrapple(int id, bool enable, Vector3 grapplePoint)
        {
            if (!Recording) return;
            
            if (RecordDemo.EntityList.TryGetValue(id, out var entityData))
            {
                DemoPlayerData playerData = (DemoPlayerData)entityData;
                playerData.GrappleDatas[RecordTick] = new DemoPlayerData.GrappleData(enable, grapplePoint);
            }
        }

        public void AddPlayerTakeDamage(int id,int attackerId, int damage, int health, bool headShot, Vector3 pos, Vector3 normal)
        {
            if (!Recording) return;
            
            if (RecordDemo.EntityList.TryGetValue(id, out var entityData))
            {
                DemoPlayerData playerData = (DemoPlayerData)entityData;
                playerData.DamageTaken[RecordTick] = new DemoPlayerData.DamageTakenData(attackerId,damage,health, headShot, pos, normal);
            }
        }
        
        public void AddPlayerTakeHealth(int id,int health,int maxHealth)
        {
            if (!Recording) return;
            
            if (RecordDemo.EntityList.TryGetValue(id, out var entityData))
            {
                DemoPlayerData playerData = (DemoPlayerData)entityData;
                playerData.HealthTaken[RecordTick] = new DemoPlayerData.HealthTakenData(health, maxHealth);
            }
        }
        
        public void AddPlayerState(int id, bool dead, bool infected, int attackerId, ushort weaponIndex)
        {
            if (!Recording) return;
            
            if (RecordDemo.EntityList.TryGetValue(id, out var entityData))
            {
                DemoPlayerData playerData = (DemoPlayerData)entityData;
                int state = dead ? 0 : infected ? 2 : 1;
                playerData.State[RecordTick] = new DemoPlayerData.StateData(state,attackerId,weaponIndex);
            }
        }

        #endregion

        #region Throwable

        public void AddThrowableSpawned(ClientGrenade throwable)
        {
            if (!Recording) return;

            int id = GetNewId();
            
            Vector3 pos = throwable.DemoThrowable.GetPosition();

            if (ClientPlayer.list.TryGetValue(throwable.PlayerId, out var clientPlayer))
            {
                DemoThrowableData data = new DemoThrowableData(id, RecordTick, pos,clientPlayer.demoPlayer.Id ,(int)throwable.type,
                    throwable.ThrowDirection)
                {
                    Positions =
                    {
                        [RecordTick] = pos.ToMyVector3()
                    }
                };

                RecordDemo.EntityList.Add(id, data);

                throwable.DemoThrowable.Spawn(id, throwable.type, pos);

                Entities[id] = throwable.DemoThrowable;
            }
        }

        #endregion

        #region Enemy

        public void AddEnemySpawned(ClientEnemy enemy)
        {
            if (!Recording) return;

            int id = GetNewId();
            
            Vector3 pos = enemy.demoEnemy.GetPosition();

            DemoEnemyData data;

            switch (enemy.enemyState.enemyType)
            {
                case ServerEnemy.EnemyType.Jack:
                    data = new DemoJackData(id, RecordTick, pos,(int)enemy.enemyState.enemyType ,enemy.index)
                    {
                        States =
                        {
                            [RecordTick] = (int)((JackState)enemy.enemyState).state
                        }
                    };
                    break;
                case ServerEnemy.EnemyType.Zombie:
                    data = new DemoZombieData(id, RecordTick, pos,(int)enemy.enemyState.enemyType ,enemy.index)
                    {
                        States =
                        {
                            [RecordTick] = (int)((ZombieState)enemy.enemyState).state
                        }
                    };
                    break;
                case ServerEnemy.EnemyType.Kat:
                    data = new DemoKatData(id, RecordTick, pos,(int)enemy.enemyState.enemyType ,enemy.index)
                    {
                        States =
                        {
                            [RecordTick] = new DemoKatData.DemoKatStateData((int)((KatState)enemy.enemyState).state, -1)
                        }
                    };
                    break;
                case ServerEnemy.EnemyType.Turret:
                    data = new DemoTurretData(id, RecordTick, pos,(int)enemy.enemyState.enemyType ,enemy.index)
                    {
                        States =
                        {
                            [RecordTick] = new DemoTurretData.DemoTurretStateData((int)((TurretState)enemy.enemyState).state, -1)
                        }
                    };
                    break;
                default:
                    return;
            }

            data.Positions[RecordTick] = pos.ToMyVector3();
            data.Rotations[RecordTick] = enemy.demoEnemy.GetEulerAngleY();

            RecordDemo.EntityList.Add(id, data);

            enemy.demoEnemy.Spawn(id, enemy.enemyState.enemyType);

            Entities[id] = enemy.demoEnemy;
        }
        
        public void AddEnemyTakeDamage(int id, int health, int attackerId)
        {
            if (!Recording) return;
            
            if (RecordDemo.EntityList.TryGetValue(id, out var entityData))
            {
                DemoEnemyData enemyData = (DemoEnemyData)entityData;
                enemyData.DamageTaken[RecordTick] = new DemoEnemyData.DamageTakenData(health,attackerId);
            }
        }
        
        public void AddEnemyState(int id, int state)
        {
            if (!Recording) return;
            
            if (RecordDemo.EntityList.TryGetValue(id, out var entityData))
            {
                DemoEnemyData enemyData = (DemoEnemyData)entityData;

                switch (enemyData.enemyType)
                {
                    case 1:
                        DemoJackData jackData = (DemoJackData)enemyData;
                        jackData.States[RecordTick] = state;
                        break;
                    case 2: //Zombie
                        DemoZombieData zombieData = (DemoZombieData)enemyData;
                        zombieData.States[RecordTick] = state;
                        break;
                }
            }
        }
        
        public void AddEnemyStateTarget(int id, int state, int targetId)
        {
            if (!Recording) return;
            
            if (RecordDemo.EntityList.TryGetValue(id, out var entityData))
            {
                DemoEnemyData enemyData = (DemoEnemyData)entityData;

                switch (enemyData.enemyType)
                {
                    case 3: //Turret
                        DemoTurretData turretData = (DemoTurretData)enemyData;
                        turretData.States[RecordTick] = new(state,targetId);
                        break;
                    case 4: //Kat
                        DemoKatData katData = (DemoKatData)enemyData;
                        katData.States[RecordTick] = new DemoKatData.DemoKatStateData(state,targetId);
                        break;
                }
            }
        }

        #endregion

        private static int GetNewId()
        {
            if (Recording)
            {
                return RecordDemo.EntityList.Count;
            }

            return 0;
        }

        #endregion

        #region Replay

        public void SkipSeconds(int second = 2)
        {
            ReplayTick += (int)(second / 0.02f);
            DoReplaying(true);
        }

        public void SetReplayTick(int tick)
        {
            ReplayTick = tick;

            DoReplaying(true);
        }

        void DestroyEntity(DemoEntity entity, bool visual)
        {
            if (entity.IsEnemy())
            {
                entity.Destroy(visual,((DemoEnemy)entity).attackerId);
            }
            else
            {
                entity.Destroy(visual);
            }
        }

        void CheckEntityData()
        {
            foreach (var entityData in ReplayDemo.EntityList.Values)
            {
                if (Entities.TryGetValue(entityData.id, out var entity))
                {
                    if (entityData.destroyTick <= ReplayTick)
                    {
                        DestroyEntity(entity,true);
                    }
                    else
                    {
                        if (entity.IsDestroyed && ReplayTick >= entityData.spawnTick)
                        {
                            if (entity.IsThrowable())
                            {
                                DemoThrowableData throwableData = (DemoThrowableData)entityData;

                                DemoThrowable throwable = (DemoThrowable)entity;

                                throwable.GetTransform().position = throwableData.position.ToVector3();
                                
                                throwable.Active();

                                if (TryToGetEntityCollider(throwableData.playerId, out var col))
                                {
                                    throwable.Spawn(throwableData.id,(ThrowObjectMenu.ThrowObjectType)throwableData.throwableType,throwableData.throwDirection, col);
                                }
                            }
                            else if (entityData.IsPlayer())
                            {
                                DemoPlayerData playerData = (DemoPlayerData)entityData;

                                DemoPlayer player = (DemoPlayer)entity;

                                player.GetTransform().position = playerData.position.ToVector3();
                                
                                player.Active();

                                player.Spawn(entityData.id, playerData.username, playerData.steamId, playerData.defaultWeaponData.currentWeaponIndex, playerData.defaultWeaponData.weapons, playerData.cosmeticIndex.ToCosmeticIndex(), playerData.isInfected);
                            }
                            else if(entityData.IsEnemy())
                            {
                                DemoEnemyData enemyData = (DemoEnemyData)entityData;

                                DemoEnemy enemy = (DemoEnemy)entity;

                                enemy.GetTransform().position = enemyData.position.ToVector3();
                                
                                enemy.Active();
                                
                                enemy.Spawn(enemyData.id, enemyData.enemyType);
                            }
                        }
                        else if (!entity.IsDestroyed && ReplayTick < entityData.spawnTick)
                        {
                            DestroyEntity(entity,false);
                        }
                        else if(!entity.IsDestroyed)
                        {
                            if (entity.IsPlayer())
                            {
                                DemoPlayerData playerData = (DemoPlayerData)entityData;
                                DemoPlayer player = (DemoPlayer)entity;
                                PlayerReplay(playerData,player);
                            }
                            else if (entity.IsThrowable())
                            {
                                DemoThrowable throwable = (DemoThrowable)entity;
                                throwable.TryToSetCurrentVelocity();
                            }
                            else if (entity.IsEnemy())
                            {
                                DemoEnemyData enemyData = (DemoEnemyData)entityData;
                                DemoEnemy enemy = (DemoEnemy)entity;
                                EnemyReplay(enemy,enemyData);
                            }
                        }
                    }
                }
                else
                {
                    if (ReplayTick >= entityData.spawnTick)
                    {
                        if (entityData.IsPlayer())
                        {
                            DemoPlayerData playerData = (DemoPlayerData)entityData;
                            
                            var player = Instantiate(NetworkManager.Instance.PlayerPrefab, entityData.position.ToVector3(),
                                    Quaternion.identity)
                                .GetComponent<DemoPlayer>();

                            player.Spawn(entityData.id, playerData.username, playerData.steamId, playerData.defaultWeaponData.currentWeaponIndex, playerData.defaultWeaponData.weapons, playerData.cosmeticIndex.ToCosmeticIndex(), playerData.isInfected);

                            Entities.Add(entityData.id, player);
                        }
                        else if (entityData.IsThrowable())
                        {
                            DemoThrowableData throwableData = (DemoThrowableData)entityData;

                            if (!throwableData.position.ToVector3().CheckIsTooLarge())
                            {
                                if (TryToGetEntityCollider(throwableData.playerId, out var col))
                                {
                                    DemoThrowable obj = Throwable
                                        .InstantiateThrowable((ThrowObjectMenu.ThrowObjectType)throwableData.throwableType,
                                            throwableData.position.ToVector3()).GetComponent<DemoThrowable>();

                                    obj.Spawn(throwableData.id,
                                        (ThrowObjectMenu.ThrowObjectType)throwableData.throwableType,
                                        throwableData.throwDirection, col);

                                    Entities.Add(entityData.id, obj);
                                }
                            }
                        }
                        else if (entityData.IsEnemy())
                        {
                            DemoEnemyData enemyData = (DemoEnemyData)entityData;

                            var obj = EnemyState.InstantiateEnemy((ServerEnemy.EnemyType)enemyData.enemyType,
                                enemyData.position.ToVector3(), enemyData.index).GetComponent<DemoEnemy>();
                            
                            obj.Spawn(enemyData.id, enemyData.enemyType);
                            
                            Entities.Add(entityData.id, obj);
                        }
                    }
                }
            }
        }

        #region Player

        void PlayerReplay(DemoPlayerData playerData, DemoPlayer player)
        {
            PlayerGrappleReplay(playerData,player);
            
            if (playerData.State.TryGetValue(ReplayTick, out var state))
            {
                switch (state.state)
                {
                    case 0:
                        player.SetDead(playerData,state);
                        break;
                    case 1:
                        player.playerState.IsInfected = false;
                        player.Respawn();
                        break;
                    case 2:
                        player.playerState.IsInfected = true;
                        player.Respawn();
                        break;
                }
            }
            else if(PreciseDemo)
            {
                foreach (var pair in playerData.State.OrderBy(item => Math.Abs(ReplayTick - item.Key)))
                {
                    if(ReplayTick < pair.Key) continue;
                    
                    if (player.Dead)
                    {
                        switch (pair.Value.state)
                        {
                            case 1:
                                player.playerState.IsInfected = false;
                                player.Respawn();
                                break;
                            case 2:
                                player.playerState.IsInfected = true;
                                player.Respawn();
                                break;
                        }
                    }
                    else
                    {
                        if (player.playerState.IsInfected)
                        {
                            switch (pair.Value.state)
                            {
                                case 0:
                                    player.SetDead(playerData,pair.Value);
                                    break;
                                case 1:
                                    player.playerState.IsInfected = false;
                                    player.Respawn();
                                    break;
                            }
                        }
                        else
                        {
                            switch (pair.Value.state)
                            {
                                case 0:
                                    player.SetDead(playerData,pair.Value);
                                    break;
                                case 2:
                                    player.playerState.IsInfected = true;
                                    player.Respawn();
                                    break;
                            }
                        }
                        
                    }
                    break;
                }
            }

            if (playerData.Crouches.TryGetValue(ReplayTick, out var crouch))
            {
                player.SetCrouch(crouch);
            }
            
            if (playerData.Aiming.TryGetValue(ReplayTick, out var aiming))
            {
                player.playerState.SetAiming(aiming);
            }
            else if (PreciseDemo)
            {
                foreach (var pair in playerData.Aiming.OrderBy(item => Math.Abs(ReplayTick - item.Key)))
                {
                    if(ReplayTick < pair.Key) continue;

                    if (player.playerState.Aiming)
                    {
                        if (!pair.Value)
                        {
                            player.playerState.SetAiming(pair.Value);
                        }
                    }
                    else
                    {
                        if (pair.Value)
                        {
                            player.playerState.SetAiming(pair.Value);
                        }
                    }
                    break;
                }
            }

            if (playerData.WeaponUpdates.TryGetValue(ReplayTick, out var weaponUpdateData))
            {
                player.playerState.WeaponManager.UpdateWeapons(weaponUpdateData.weapons, weaponUpdateData.currentWeaponIndex, weaponUpdateData.weaponSkinIndexes);
            }

            if (playerData.WeaponSwitched.TryGetValue(ReplayTick, out var weaponSwitchedData))
            {
                player.playerState.WeaponManager.SwitchWeapon(weaponSwitchedData);
            }

            if (playerData.WeaponShooting.TryGetValue(ReplayTick, out var shootData))
            {
                if (player.playerState.WeaponManager.CurrentWeaponIndex != shootData.currentWeaponIndex)
                {
                    player.playerState.WeaponManager.SwitchWeapon(shootData.currentWeaponIndex);
                }
                player.playerState.WeaponManager.Shoot(shootData.direction, shootData.bulletCount, EShootingResult.EResultOk);
            }

            if (playerData.WeaponReload.TryGetValue(ReplayTick, out var reloadData))
            {
                player.playerState.WeaponManager.StartReloading(reloadData.currentWeaponIndex);
            }

            if (playerData.DamageTaken.TryGetValue(ReplayTick, out var damageTakenData))
            {
                player.playerState.SetHealth(damageTakenData.health,damageTakenData.damage,damageTakenData.headShot,damageTakenData.position,damageTakenData.normal,true,damageTakenData.attackerId != playerData.id && ((DemoPlayer)Entities[damageTakenData.attackerId]).playerState.IsLocal);
            }
            
            if (playerData.HealthTaken.TryGetValue(ReplayTick, out var healthTakenData))
            {
                player.playerState.TakeHealth(healthTakenData.health,healthTakenData.maxHealth);
            }

            Vector3 position = playerData.Positions[ReplayTick].ToVector3();
            Vector3 offset = player.Crouch ? new Vector3(0, 0.33f, 0) : Vector3.zero;
            player.NewPosition(position + offset);
            player.NewRotation(playerData.Rotations[ReplayTick], playerData.HeadRotations[ReplayTick]);

            // Derive the velocity from the recorded movement. The recorded rigidbody
            // velocity is only meaningful for the local player (remote players are
            // moved by interpolation, not physics), so using it would leave remote
            // players without any weapon bob / movement feedback during playback.
            Vector3 velocity = Time.fixedDeltaTime > 0f
                               && playerData.Positions.TryGetValue(ReplayTick - 1, out var previousPosition)
                ? (position - previousPosition.ToVector3()) / Time.fixedDeltaTime
                : Vector3.zero;
            player.NewVelocity(velocity);
        }

        void PlayerGrappleReplay(DemoPlayerData playerData, DemoPlayer player)
        {
            if (playerData.GrappleDatas.TryGetValue(ReplayTick, out var grappleData))
            {
                if (grappleData.enable)
                {
                    player.playerState.grappling.StartGrapple(grappleData.grapplePoint);
                }
                else
                {
                    player.playerState.grappling.StopGrapple();
                }
            }
            else if(PreciseDemo)
            {
                foreach (var pair in playerData.GrappleDatas.OrderBy(item => Math.Abs(ReplayTick - item.Key)))
                {
                    if(ReplayTick < pair.Key) continue;
                    
                    if (player.playerState.grappling.IsGrappling())
                    {
                        if (!pair.Value.enable)
                        {
                            player.playerState.grappling.StopGrapple();
                        }
                    }
                    else
                    {
                        if (pair.Value.enable)
                        {
                            player.playerState.grappling.StartGrapple(pair.Value.grapplePoint);
                        }
                    }
                    break;
                }
            }
        }

        #endregion

        #region Enemy

        void EnemyReplay(DemoEnemy enemy, DemoEnemyData enemyData)
        {
            if (enemyData.DamageTaken.TryGetValue(ReplayTick, out var damageTakenData))
            {
                enemy.enemyState.SetHealth(damageTakenData.health, damageTakenData.attackerId,true);
            }

            switch (enemyData.enemyType)
            {
                case 1:
                    JackState jackState = (JackState)enemy.enemyState;
                    DemoJackData jackData = (DemoJackData)enemyData;
                    if (jackData.States.TryGetValue(ReplayTick, out var state))
                    {
                        jackState.SetState((ServerJack.EJackState)state, (JackAnimation)enemy.Animation);
                    }
                    else if(PreciseDemo)
                    {
                        foreach (var pair in jackData.States.OrderBy(item => Math.Abs(ReplayTick - item.Key)))
                        {
                            if(ReplayTick < pair.Key) continue;

                            ServerJack.EJackState eJackState = (ServerJack.EJackState)pair.Value;
                    
                            if (jackState.state != eJackState)
                            {
                                jackState.SetState(eJackState, (JackAnimation)enemy.Animation);
                            }
                            break;
                        }
                    }
                    break;
                case 2:
                    ZombieState zombieState = (ZombieState)enemy.enemyState;
                    DemoZombieData zombieData = (DemoZombieData)enemyData;
                    if (zombieData.States.TryGetValue(ReplayTick, out var zombieStateIndex))
                    {
                        zombieState.SetState((ServerZombie.EZombieState)zombieStateIndex);
                    }
                    else if(PreciseDemo)
                    {
                        foreach (var pair in zombieData.States.OrderBy(item => Math.Abs(ReplayTick - item.Key)))
                        {
                            if(ReplayTick < pair.Key) continue;

                            ServerZombie.EZombieState eZombieState = (ServerZombie.EZombieState)pair.Value;
                    
                            if (zombieState.state != eZombieState)
                            {
                                zombieState.SetState(eZombieState);
                            }
                            break;
                        }
                    }
                    break;
                case 3:
                    TurretState turretState = (TurretState)enemy.enemyState;
                    DemoTurretData turretData = (DemoTurretData)enemyData;
                    if (turretData.States.TryGetValue(ReplayTick, out var turretStateData))
                    {
                        Transform target=null;
                        if (Entities.TryGetValue(turretStateData.targetId, out var entity))
                        {
                            target = entity.GetTransform();
                        }
                        turretState.SetState((ServerTurret.ETurretState)turretStateData.state, target);
                    }
                    else if(PreciseDemo)
                    {
                        foreach (var pair in turretData.States.OrderBy(item => Math.Abs(ReplayTick - item.Key)))
                        {
                            if(ReplayTick < pair.Key) continue;

                            ServerTurret.ETurretState eTurretState = (ServerTurret.ETurretState)pair.Value.state;
                    
                            if (turretState.state != eTurretState)
                            {
                                Transform target=null;
                                if (Entities.TryGetValue(pair.Value.targetId, out var entity))
                                {
                                    target = entity.GetTransform();
                                }
                                turretState.SetState(eTurretState, target);
                            }
                            break;
                        }
                    }
                    break;
                case 4:
                    KatState katState = (KatState)enemy.enemyState;
                    DemoKatData katData = (DemoKatData)enemyData;
                    if (katData.States.TryGetValue(ReplayTick, out var katStateData))
                    {
                        Transform target=null;
                        if (Entities.TryGetValue(katStateData.targetId, out var entity))
                        {
                            target = entity.GetTransform();
                        }
                        katState.SetState((ServerKat.EKatState)katStateData.state, target);
                    }
                    else if(PreciseDemo)
                    {
                        foreach (var pair in katData.States.OrderBy(item => Math.Abs(ReplayTick - item.Key)))
                        {
                            if(ReplayTick < pair.Key) continue;

                            ServerKat.EKatState eKatState = (ServerKat.EKatState)pair.Value.state;
                    
                            if (katState.state != eKatState)
                            {
                                Transform target=null;
                                if (Entities.TryGetValue(pair.Value.targetId, out var entity))
                                {
                                    target = entity.GetTransform();
                                }
                                katState.SetState(eKatState, target);
                            }
                            break;
                        }
                    }
                    break;
            }

            enemy.NewPosition(enemyData.Positions[ReplayTick].ToVector3());
            enemy.NewRotation(enemyData.Rotations[ReplayTick]);
        }

        #endregion

        #endregion

        public DemoEntity GetEntity(int id)
        {
            if (Entities.TryGetValue(id, out var entity))
            {
                return entity;
            }

            return null;
        }

        public bool TryGetEntity(int id, out DemoEntity entity)
        {
            if (Entities.TryGetValue(id, out entity))
            {
                return true;
            }

            return false;
        }

        bool TryToGetEntityCollider(int id, out Collider col)
        {
            if (Entities.TryGetValue(id, out var entity))
            {
                col = entity.GetComponentInChildren<Collider>();
                return true;
            }

            col = null;
            return false;
        }

        public void DestroyEntity(int id)
        {
            if (!Recording) return;
            
            if (Entities.ContainsKey(id))
            {
                if (RecordDemo.EntityList.TryGetValue(id, out var entityData))
                {
                    entityData.Destroy(RecordTick);
                }
                Entities.Remove(id);
            }
        }
        
        public void DestroyEnemy(int id, int attackerId )
        {
            if (!Recording) return;
            
            if (Entities.ContainsKey(id))
            {
                if (RecordDemo.EntityList.TryGetValue(id, out var entityData))
                {
                    if (entityData.IsEnemy())
                    {
                        ((DemoEnemyData)entityData).SetAttacker(attackerId);
                    }
                    entityData.Destroy(RecordTick);
                }
                Entities.Remove(id);
            }
        }

        public void DestroyEverything()
        {
            if (!Recording) return;
            
            for (int i = 0; i < Entities.Values.Count; i++)
            {
                var entity = Entities.Values.ElementAt(i);
                if (RecordDemo.EntityList.TryGetValue(entity.Id, out var entityData))
                {
                    entityData.Destroy(RecordTick);
                }
                Entities.Remove(entity.Id);
            }
        }

        public void RemoveDemo(int index)
        {
            if (index < DemoFiles.Length)
            {
                var file = DemoFiles[index];
                DemoFiles = DemoFiles.Where(e => e != file).ToArray();
                
                file.Delete();
            }
        }

        public void PlayDemo(int index)
        {
            Entities.Clear();
            if (index < DemoFiles.Length)
            {
                StartCoroutine(ReadDemoFile(index));
            }
        }

        public void StopPlayDemo()
        {
            ReplayDemo = null;

            Replaying = false;
            
            DemoCanvas.Instance.DeInitialize();
        }

        IEnumerator ReadDemoFile(int index)
        {
            float startTime = Time.time;
            
            CoroutineWithData cd = new CoroutineWithData(this,
                SaveSystem.ReadFileAsyncThread(DemoFiles[index].FullName));
            
            OnDemoLoad?.Invoke(true);
            
            yield return cd.coroutine;

            DeserializeDemoFile(index,cd.result.ToString(), startTime);
        }

        async void DeserializeDemoFile(int index, string demo, float startTime)
        {
            try
            {
                ReplayDemo = await Task.Run((() => JsonConvert.DeserializeObject<DemoData>(demo,_jsonSerializerSettings)));
                OnDemoLoad?.Invoke(false);
            }
            catch (Exception e)
            {
                FailedWindow window = Instantiate(PrefabManager.Instance.failedWindow, UIManager.Instance.transform)
                    .GetComponent<FailedWindow>();
                window.SetTitle("Demo Loading Failed");
                window.SetReason(e.Message);
                Debug.LogError(e.Message);
                OnDemoLoad?.Invoke(false);
                return;
            }

            ReplayDemo.Name = DemoFiles[index].Name;
            ReplayDemo.DateTime = DemoFiles[index].LastWriteTime;
                
            Debug.Log($"Play Demo {ReplayDemo.mapId}, Loading Time: {Time.time - startTime}");

            //Load Map Or Workshop Map
            LoadingManager.Instance.StartCoroutine(ReplayDemo.workshopMap
                ? LoadingManager.Instance.PlayWorkshopMapDemo((PublishedFileId_t)ulong.Parse(ReplayDemo.mapId), OnSceneLoaded) : LoadingManager.Instance.PlayDemo(ReplayDemo.mapId,
                    MapManager.Instance.GetMapTexture(ReplayDemo.mapId), OnSceneLoaded));
        }

        void OnSceneLoaded()
        {
            Replaying = true;
            
            ReplayTick = 0;
            
            DemoCanvas.Instance.Initialize(ReplayDemo);
        }

        #region Record / Stop Record / Save / Load

        public bool StartRecord(string saveName)
        {
            if (!NetworkManager.Instance.Client.IsConnected || NetworkManager.GameState == GameState.Voting ||
                NetworkManager.GameState == GameState.None || string.IsNullOrEmpty(saveName) || Recording)
            {
                Debug.Log("Failed to start demo recording");
                return false;
            }
            
            string mapId = NetworkManager.Instance.MapId;

            if (string.IsNullOrEmpty(mapId))
            {
                Debug.Log("Failed to start demo recording, reason: invalid map");
                return false;
            }

            _saveName = saveName;

            RecordTick = 0;
            Recording = true;
            
            OnRecordingChanged?.Invoke(Recording);
            
            Entities.Clear();

            RecordDemo = new DemoData(mapId,NetworkManager.Instance.IsWorkshopMap);

            foreach (var player in ClientPlayer.list.Values)
            {
                AddPlayerSpawned(player);
            }

            foreach (var throwable in ClientGrenade.list.Values)
            {
                AddThrowableSpawned(throwable);
            }

            foreach (var enemy in ClientEnemy.list.Values)
            {
                AddEnemySpawned(enemy);
            }

            Debug.Log("Start demo recording");
            return true;
        }

        public bool StopRecord(bool debug=false)
        {
            if (!Recording || RecordDemo == null)
            {
                if(debug)
                    Debug.LogError("Stop recording failed, no record demo file found");
                return false;
            }

            Save();

            Recording = false;
            
            OnRecordingChanged?.Invoke(Recording);

            RecordDemo = null;

            return true;
        }

        public bool Save() //Save the current record data
        {
            if (!Recording)
            {
                Debug.LogError("You are not recording, no object to save");
                return false;
            }

            if (RecordDemo == null)
            {
                Debug.LogError("Demo is null, no object to save");
                return false;
            }

            RecordDemo.endTick = RecordTick;

            string data = JsonConvert.SerializeObject(RecordDemo, Formatting.Indented, _jsonSerializerSettings);

            string path = Path.Combine(SavePath, $"{_saveName}.dem");
            
            SaveSystem.WriteFile(path, data);
            
            Debug.Log($"{path} Saved Successfully");

            return true;
        }

        #endregion
    }

    [Serializable]
    public class DemoData
    {
        [JsonIgnore]
        public string Name;

        [JsonIgnore]
        public DateTime DateTime;
        
        public string mapId;

        public bool workshopMap;

        public int endTick;

        public DemoData(string mapId, bool workshopMap)
        {
            this.mapId = mapId;
            this.workshopMap = workshopMap;
        }

        [JsonConstructor]
        public DemoData()
        {
            mapId = String.Empty;
            workshopMap = false;
            EntityList = new Dictionary<int, DemoEntityData>();
        }

        #region Entity

        public Dictionary<int, DemoEntityData> EntityList = new();

        #endregion
    }

    [Serializable]
    public abstract class DemoEntityData
    {
        public int id;

        public int spawnTick;
        public int destroyTick = Int32.MaxValue;

        public MyVector3 position;
        
        public SortedList<int, MyVector3> Positions = new();
        
        public void Destroy(int tick)
        {
            destroyTick = tick;
        }

        public virtual bool IsPlayer()
        {
            return false;
        }
        
        public virtual bool IsEnemy()
        {
            return false;
        }
        
        public virtual bool IsThrowable()
        {
            return false;
        }
    }
    
    [Serializable]
    public class DemoPlayerData : DemoEntityData
    {
        public int type = 1;

        public string username;

        public ulong steamId;

        public bool isInfected = false;

        public DemoCosmeticIndex cosmeticIndex;

        public WeaponUpdateData defaultWeaponData;
        
        public SortedList<int, float> Rotations = new();
        public SortedList<int, float> HeadRotations = new();
        public SortedList<int, MyVector3> Velocities = new();
        public SortedList<int, bool> Crouches = new();
        public SortedList<int, bool> Aiming = new();

        public SortedList<int, StateData> State = new(); //Alive or dead

        public SortedList<int, DamageTakenData> DamageTaken = new();
        public SortedList<int, HealthTakenData> HealthTaken = new();
        
        public SortedList<int, WeaponUpdateData> WeaponUpdates = new();
        public SortedList<int, int> WeaponSwitched = new();
        public SortedList<int, WeaponShootData> WeaponShooting = new();
        public SortedList<int, WeaponReloadData> WeaponReload = new();
        
        public SortedList<int, GrappleData> GrappleDatas = new();

        public DemoPlayerData(int id,int spawnTick, string username, ulong steamId, Vector3 position, int currentWeaponIndex, short[] weapons, InventoryManager.CosmeticIndex cosmeticIndex, bool isInfected)
        {
            this.id = id;
            this.spawnTick = spawnTick;
            this.username = username;
            this.position = position.ToMyVector3();
            this.steamId = steamId;
            this.isInfected = isInfected;
            this.cosmeticIndex = new DemoCosmeticIndex(cosmeticIndex);
            defaultWeaponData = new WeaponUpdateData(currentWeaponIndex, weapons, cosmeticIndex.weaponIndex);
        }

        public DemoPlayerData()
        {
            username = String.Empty;
            position = Vector3.zero.ToMyVector3();
            steamId = 0;
            destroyTick = int.MaxValue;
        }

        [Serializable]
        public class WeaponUpdateData
        {
            public int currentWeaponIndex;

            public short[] weapons;

            public ushort[] weaponSkinIndexes;

            public WeaponUpdateData(int currentWeaponIndex, short[] weapons, ushort[] weaponSkinIndexes)
            {
                this.currentWeaponIndex = currentWeaponIndex;
                this.weapons = weapons;
                this.weaponSkinIndexes = weaponSkinIndexes;
            }
        }
        
        [Serializable]
        public class WeaponShootData
        {
            public int currentWeaponIndex; // TO ensure that the weapon is correct

            public Vector3 direction;

            public int bulletCount;

            public WeaponShootData(int currentWeaponIndex, Vector3 direction, int bulletCount)
            {
                this.currentWeaponIndex = currentWeaponIndex;
                this.direction = direction;
                this.bulletCount = bulletCount;
            }
        }
        
        [Serializable]
        public class WeaponReloadData
        {
            public int currentWeaponIndex;

            public int bulletLeft;

            public WeaponReloadData(int currentWeaponIndex, int bulletLeft)
            {
                this.currentWeaponIndex = currentWeaponIndex;
                this.bulletLeft = bulletLeft;
            }
        }
        
        [Serializable]
        public class GrappleData
        {
            public bool enable;

            public Vector3 grapplePoint;

            public GrappleData(bool enable, Vector3 grapplePoint)
            {
                this.enable = enable;
                this.grapplePoint = grapplePoint;
            }
        }
        
        [Serializable]
        public class DamageTakenData
        {
            public int attackerId = 0;
            
            public int damage,health;

            public bool headShot;

            public Vector3 position, normal;

            public DamageTakenData(int attackerId,int damage,int health, bool headShot, Vector3 position, Vector3 normal)
            {
                this.attackerId = attackerId;
                this.damage = damage;
                this.health = health;
                this.headShot = headShot;
                this.position = position;
                this.normal = normal;
            }
        }
        
        [Serializable]
        public class HealthTakenData
        {
            public int health,maxHealth=100;

            public HealthTakenData(int health, int maxHealth)
            {
                this.health = health;   
                this.maxHealth = maxHealth;   
            }
            
            public HealthTakenData()
            {
            
            }
        }
        
        /// <summary>
        /// Use to record dead or alive state (includes attacker id, weapon index)
        /// </summary>
        [Serializable]
        public class StateData
        {
            public int state = 0; // 0 -- dead, 1 -- alive, 2 -- infected
            public int attackerId;
            public ushort weaponIndex;
            public bool headShot,wallbang;

            public StateData(int state, int attackerId, ushort weaponIndex)
            {
                this.state = state;
                this.attackerId = attackerId;
                this.weaponIndex = weaponIndex;
            }
        }
        
        [Serializable]
        public class ChatData
        {
            public ulong steamId;
            public string text;

            public ChatData(ulong steamId,string text)
            {
                this.steamId = steamId;
                this.text = text;
            }
        }
        
        public override bool IsPlayer()
        {
            return true;
        }
    }

    [Serializable]
    public class DemoThrowableData : DemoEntityData
    {
        public int type = 2;

        public int playerId;
        
        public int throwableType;

        public Vector3 throwDirection;

        public DemoThrowableData(int id,int spawnTick, Vector3 position, int playerId, int throwableType, Vector3 throwDirection)
        {
            base.id = id;
            base.spawnTick = spawnTick;
            base.position = position.ToMyVector3();
            this.playerId = playerId;
            this.throwableType = throwableType;
            this.throwDirection = throwDirection;
        }

        public override bool IsThrowable()
        {
            return true;
        }
    }

    [Serializable]
    public abstract class DemoEnemyData : DemoEntityData
    {
        public int type = 3;

        public int enemyType = 0, index = 0;

        public int attackerId;
        
        public SortedList<int, float> Rotations = new();
        public SortedList<int, DamageTakenData> DamageTaken = new(); // the health
        protected DemoEnemyData(int id,int spawnTick, Vector3 position, int enemyType, int index)
        {
            base.id = id;
            base.spawnTick = spawnTick;
            base.position = position.ToMyVector3();
            this.enemyType = enemyType;
            this.index = index;
        }

        public void SetAttacker(int attacker)
        {
            attackerId = attacker;
        }
        
        [Serializable]
        public class DamageTakenData
        {
            public int health = 0;
            public int attackerId;

            public DamageTakenData(int health,int attackerId)
            {
                this.health = health;
                this.attackerId = attackerId;
            }
        }

        public override bool IsEnemy()
        {
            return true;
        }
    }
    
    [Serializable]
    public class DemoJackData : DemoEnemyData
    {
        public SortedList<int, int> States = new();
        
        public DemoJackData(int id, int spawnTick, Vector3 position, int enemyType, int index) : base(id, spawnTick, position, enemyType, index)
        {
            
        }
    }
    [Serializable]
    public class DemoZombieData : DemoEnemyData
    {
        public SortedList<int, int> States = new();
        
        public DemoZombieData(int id, int spawnTick, Vector3 position, int enemyType, int index) : base(id, spawnTick, position, enemyType, index)
        {
            
        }
    }
    [Serializable]
    public class DemoKatData : DemoEnemyData
    {
        public SortedList<int, DemoKatStateData> States = new();
        
        public DemoKatData(int id, int spawnTick, Vector3 position, int enemyType, int index) : base(id, spawnTick, position, enemyType, index)
        {
            
        }
        
        [Serializable]
        public class DemoKatStateData
        {
            public int state;

            public int targetId = -1;

            public DemoKatStateData(int state, int targetId)
            {
                this.state = state;
                this.targetId = targetId;
            }

            public DemoKatStateData()
            {
                
            }
        }
    }
    [Serializable]
    public class DemoTurretData : DemoEnemyData
    {
        public SortedList<int, DemoTurretStateData> States = new();
        
        public DemoTurretData(int id, int spawnTick, Vector3 position, int enemyType, int index) : base(id, spawnTick, position, enemyType, index)
        {
            
        }
        
        [Serializable]
        public class DemoTurretStateData
        {
            public int state;

            public int targetId = -1;

            public DemoTurretStateData(int state, int targetId)
            {
                this.state = state;
                this.targetId = targetId;
            }

            public DemoTurretStateData()
            {
                
            }
        }
    }
    public class DemoEntityDataConverter : JsonConverter<DemoEntityData>
    {
        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, DemoEntityData? value, JsonSerializer serializer) => throw new NotImplementedException();

        public override DemoEntityData ReadJson(JsonReader reader, Type objectType, DemoEntityData existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            // TODO handle nulls
            var jObject = JObject.Load(reader);
            DemoEntityData result;
            if(jObject.TryGetValue("type", StringComparison.InvariantCultureIgnoreCase, out var value))
            {
                switch (value.ToObject<int>(serializer))
                {
                    case 1:
                        result = jObject.ToObject<DemoPlayerData>();
                        break;
                    case 2:
                        result = jObject.ToObject<DemoThrowableData>();
                        break;
                    case 3:
                        if (jObject.TryGetValue("enemyType", StringComparison.InvariantCultureIgnoreCase, out value))
                        {
                            switch (value.ToObject<int>(serializer))
                            {
                                case 1: //Jack
                                    result = jObject.ToObject<DemoJackData>();
                                    break;
                                case 2: //Zombie
                                    result = jObject.ToObject<DemoZombieData>();
                                    break;
                                case 3: //Turret
                                    result = jObject.ToObject<DemoTurretData>();
                                    break;
                                case 4: //Kat
                                    result = jObject.ToObject<DemoKatData>();
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                        else
                        {
                            throw new Exception("No enemy type found");
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                return result;
            }
            Debug.Log("No type found");
            
            return null;
        }
    }
    
    [Serializable]
    public class DemoCosmeticIndex
    {
        public int hatIndex=-1,faceIndex=-1,shoesIndex=-1,hairIndex=-1,clothesIndex=-1,pantIndex=-1;
        public int hatParticle=-1,faceParticle=-1,shoesParticle=-1,hairParticle=-1,clothesParticle=-1,pantParticle = -1;
        public MapData.MyColor hatColor=new(Color.clear), faceColor=new(Color.clear), shoesColor=new(Color.clear), hairColor=new(Color.clear), clothesColor=new(Color.clear) ,pantColor = new(Color.clear);
        public float hatShiny = 0, faceShiny=0,shoesShiny=0,hairShiny=0,clothesShiny=0,pantShiny = 0;
        
        public ushort[] weaponIndex = new ushort[30];

        public DemoCosmeticIndex(InventoryManager.CosmeticIndex cosmeticIndex)
        {
            hatIndex = cosmeticIndex.hatIndex;
            faceIndex = cosmeticIndex.faceIndex;
            shoesIndex = cosmeticIndex.shoesIndex;
            hairIndex = cosmeticIndex.hairIndex;
            pantIndex = cosmeticIndex.pantIndex;
            clothesIndex = cosmeticIndex.clothesIndex;
            hatColor = new MapData.MyColor(cosmeticIndex.hatColor);
            faceColor = new MapData.MyColor(cosmeticIndex.faceColor);
            shoesColor = new MapData.MyColor(cosmeticIndex.shoesColor);
            hairColor = new MapData.MyColor(cosmeticIndex.hairColor);
            pantColor = new MapData.MyColor(cosmeticIndex.pantColor);
            clothesColor = new MapData.MyColor(cosmeticIndex.clothesColor);
            hatShiny =cosmeticIndex. hatShiny;
            faceShiny = cosmeticIndex.faceShiny;
            shoesShiny = cosmeticIndex.shoesShiny;
            hairShiny = cosmeticIndex.hairShiny;
            clothesShiny = cosmeticIndex.clothesShiny;
            pantShiny = cosmeticIndex.pantShiny;
            weaponIndex = cosmeticIndex.weaponIndex;
            hatParticle = cosmeticIndex.hatParticle;
            faceParticle = cosmeticIndex.faceParticle;
            shoesParticle = cosmeticIndex.shoesParticle;
            hairParticle = cosmeticIndex.hairParticle;
            clothesParticle = cosmeticIndex.clothesParticle;
            pantParticle = cosmeticIndex.pantParticle;
            
        }

        public DemoCosmeticIndex()
        {
            
        }

        public InventoryManager.CosmeticIndex ToCosmeticIndex()
        {
            return new InventoryManager.CosmeticIndex(hatIndex, faceIndex, shoesIndex, hairIndex, clothesIndex,
                pantIndex, hatColor.ToColor(), faceColor.ToColor(), shoesColor.ToColor(), hairColor.ToColor(), clothesColor.ToColor(), pantColor.ToColor(), hatShiny, faceShiny, shoesShiny, hairShiny, clothesShiny, pantShiny, hatParticle, faceParticle, shoesParticle, hairParticle, clothesParticle, pantParticle, weaponIndex);
        }
    }
}