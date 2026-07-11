using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 房间类型
public enum RoomType { Start, Normal, Boss, Treasure, Shop, SafeRoom, Challenge, Curse, Secret };

// Типи випробувань (міні-ігор)
public enum ChallengeType
{
    GhostSurvival,
    ThreeCardsMonte,
    QuickTileReaction,
    SequenceMemory,
    SacrificeAltar,
    RouletteWheel,
    TimeMaze,
    CobwebDodge,
    Observer,
    BombPush,
    SkyTearsSurvival,
    ChangingSafeZones
}

// 游戏物体类型
public enum GameItemType { Item, Monster };

// 方向类型
public enum DirectionType { Up, Down, Left, Right };

// 道具池类型
public enum ItemPoolType { TreasureRoom, BossRoom, Shop };

// 武器类型
public enum GunType { Machine, Python, Java, Cpp, SQL };

// 怪物类型
public enum MonsterType { Minion, Boss };