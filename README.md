# Genies IRL

Genies IRL is an XR application for Apple Vision Pro featuring an autonomous, interactive, spatially-aware character created using Genies' interoperable avatar tech stack. This open-source project mirrors the app release to the Vision Pro App Store, searchable as 'Genies IRL'.

---

## Table of Contents

- [Description](#description)
- [Requirements](#requirements)
- [Installation](#installation)

---

## Description

Genies IRL brings character-driven XR to Apple Vision Pro, enabling rich, interactive experiences through a spatially-aware avatar powered by the Genies avatar tech stack. This example Unity project demonstrates how, using the advanced capabilities of Apple Vision Pro, a virtual character can come to life in your space.

The goal is to make interacting with XR characters feel more natural and believable, and to expand interactive possibilities in gaming and many other types of applications.

Our approach rests upon these key components:

- XR Pathfinding: The ability to navigate around walls and furniture, using spatial mesh data.
- Surface Interaction: The ability to sit on chairs, draw on walls, place objects on tables, etc.
- Brain & Senses: The ability to make sense of the world around them and prioritize actions.
- IK Subsystems: The ability to use limbs to reach at objects in the real world.
- User Interaction: The ability to interact physically with the user, by exchanging high-fives, passing objects, etc. 

In this project, we demonstrate how we can utilize a wealth of Unity libraries, as well as Aron Granberg's A* Pathfinding Project, to grant these components to a sample Genies avatar.

---

## Requirements

- **Compatible Mac computer**
- **Apple Vision Pro** with visionOS 2.0 or above
- **Unity Editor** version 6000.0.42f1 and above  
  _(Note: Higher versions may require a project upgrade)_
- **Unity Pro License** (required for Apple Vision Pro development)
- **Xcode** 1.2 and above  
  _(Note: Higher versions may require a Unity Editor upgrade for compatibility)_

---

## Installation

1. **Open the Project in Vision OS Build Mode**  
   Open the project in your Unity Editor with Vision OS build mode enabled.  
   > ⚠️ When you first open the project, you will see compiler errors due to missing dependencies.

2. **Install Dependencies: A* Pathfinding Project**  
   This project uses [Aron Granberg’s A* Pathfinding Project](https://arongranberg.com/astar/):
   - For optimal performance, the **Pro** version is recommended (supports asynchronous scanning).
   - The **Free** version is also compatible.

   - **Download Free Version:**  
     [A* Pathfinding Project Free Download](https://arongranberg.com/astar/download)
     - NOTE: Tested with version 4.2.17

   - **To Import into Unity:**  
     - In Unity, navigate to:  
       `Assets > Import Package > Custom Package`
     - Select the downloaded package.
     - _You may need to Exit Safe Mode in the Editor to access this._

   - **To Purchase Pro Version:**  
     [A* Pathfinding Project Pro (Unity Asset Store)](https://assetstore.unity.com/packages/tools/behavior-ai/a-pathfinding-project-pro-87744)  
     - Follow installation instructions provided by the package.
     - _You may need to Exit Safe Mode for this as well._
     - In Player Settings, for each platform tab, add an additional Scripting Define Symbol: "ASTAR_PRO".
     - NOTE: Tested with version 5.3.7

3. **Resolve Compiler Errors**  
   After importing the A* Pathfinding Project, compiler errors should be resolved.

4. **Open the Main Scene**  
   The main scene for the application is located at:  
   `Assets/Project/Scenes/Main`

---

## DOCUMENTATION

Please refer to GeniesIrlDocumentation.pdf inside the root folder for more information.

---

> For licensing, notices, and contributor information, see [NOTICE](NOTICE), [LICENSE](LICENSE), and [ATTRIBUTIONS.md](ATTRIBUTIONS.md).

## Contact

For inquiries or support, please contact: devrelations@genies.com
