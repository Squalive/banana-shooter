# Prerequisites

Make sure you have the following softwares installed on your computer.
  1. [Git](https://git-scm.com/)
  2. [Git LFS](https://git-lfs.com/), run `git lfs install` once before cloning
  3. [Unity 2021.3.45f2 And Unity Hub](https://unity.com/)
  4. [Steam](https://store.steampowered.com/app/1949740/Banana_Shooter/)

In order to run the game without modification in the editor, you're required to own [Banana Shooter](https://store.steampowered.com/app/1949740/Banana_Shooter/) in your steam account, which means you just need a steam account and own the game which is free. 

# Steps

## 1. Clone the project

Clone the project by:
```git clone https://github.com/Squalive/banana-shooter.git```

This process might take a while because the project contains large files including textures, models, audios (about 1.4 GB through Git LFS).

If you cloned without Git LFS, run `git lfs pull` before opening the project, otherwise Unity sees pointer files instead of assets and settings.

## 2. Opening the project in Unity

Since this project is developed using Unity, a recommended way of developing is to register the project in your Unity Hub and open the project from that.

## 3. Boot the game

Unity editor will defaults to a null scene, you can open `Assets/Scenes/LoadScene.unity` scene and hit the play button in the editor to run the game. When you want to play the game in the editor, make sure you start at the `Assets/Scenes/LoadScene.unity` scene.

Make sure steam is running on your computer.

# Completion Note

It is expected to see **errors** when opening the project for the first time. It is not expected to not being able to run the game inside the Unity Editor.

## Missing Stuff

90% of the audio files are stripped away, due to licensing issues, 10% of the effects and models are also stripped away due to licensing issues.

No code has been stripped away since its all owned by us.

There's currently no plan to revise them so basically whats working in this repo is basically the movement, weapons, maps, code.
