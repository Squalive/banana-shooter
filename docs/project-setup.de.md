# Voraussetzungen

Stell sicher, dass du die folgenden Programme auf deinem Computer installiert hast.
  1. [Git](https://git-scm.com/)
  2. [Git LFS](https://git-lfs.com/), Führe `git lfs install` einmal aus, bevor du klonst
  3. [Unity 2021.3.45f2 und Unity Hub](https://unity.com/)
  4. [Steam](https://store.steampowered.com/app/1949740/Banana_Shooter/)

Um das Spiel im Editor ohne Änderungen zu starten, musst du [Banana Shooter](https://store.steampowered.com/app/1949740/Banana_Shooter/) auf Steam besitzen, was bedeutet dass du nur einen Steam Account brauchst und das Spiel besitzen musst das kostenlos ist.

# Schritte

## 1. Klon das Projekt

Klon das Projekt mit:
```git clone https://github.com/Squalive/banana-shooter.git```

Dieser Vorgang kann eine Weile dauern, weil das Projekt große Dateien enthält darunter Texturen, Modelle und Audios (etwa 1,4 GB durch Git LFS).

Wenn du ohne Git LFS geklont hast, führe `git lfs pull` aus bevor du das Projekt öffnest sonst sieht Unity Pointer-Dateien statt Assets und Einstellungen.

## 2. Das Projekt in Unity öffnen

Da dieses Projekt mit Unity entwickelt wird ist es empfehlenswert das Projekt im Unity Hub zu registrieren und es von dort aus zu öffnen.

## 3. Starte das Spiel

Der Unity Editor startet standardmäßig mit einer null Szene. Du kannst die Szene `Assets/Scenes/LoadScene.unity` öffnen und im Editor auf den Play Button klicken um das Spiel zu starten. Wenn du das Spiel im Editor spielen willst stelle sicher dass du in der Szene `Assets/Scenes/LoadScene.unity` bist.

Stell sicher dass Steam auf deinem Computer läuft.

# Abschlussnotiz

Es wird erwartet **Fehler** zu sehen wenn man das Projekt zum ersten Mal öffnet. Es wird nicht erwartet dass man das Spiel im Unity Editor nicht ausführen kann.

## Fehlende Sachen

90% der Audiodateien wurden aufgrund von Lizenzproblemen entfernt, 10 % der Effekte und Modelle wurden ebenfalls aufgrund von Lizenzproblemen entfernt.

Kein Code wurde entfernt da er uns gehört.

Es gibt derzeit keinen Plan sie zu überarbeiten also funktioniert in diesem Repo im Grunde nur die Bewegung, die Waffen, die Karten und der Code.
