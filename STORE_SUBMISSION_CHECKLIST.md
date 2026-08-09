# 🥽 Meta Quest Store / App Lab Submission Checklist

**Project:** CueStrike — Pool Simulation for Meta Quest VR

---

## 📐 1. Technical Specifications

| Requirement | Target | Status |
|------------|--------|--------|
| **Build Target** | Android | ☐ |
| **Texture Compression** | ASTC | ☐ |
| **Scripting Backend** | IL2CPP | ☐ |
| **Target Architecture** | ARM64 | ☐ |
| **Minimum Android API** | 29 (Quest 2/3/Pro compatible) | ☐ |
| **Target Android API** | 34 | ☐ |
| **Graphics API** | Vulkan (recommended) or OpenGL ES 3.x | ☐ |
| **Frame Rate** | 72 FPS (minimum), 90 FPS (recommended) | ☐ |
| **Single-Pass Instanced** | Enabled for VR rendering | ☐ |
| **Multiview** | Enabled for performance | ☐ |
| **Quest Compatibility** | Quest 2, Quest 3, Quest Pro | ☐ |
| **Touch Controller Support** | Confirmed working | ☐ |
| **Hand Tracking Support** | (Optional) | ☐ |

---

## 🔧 2. Unity Build Configuration

### Project Settings

| Setting | Value | Location |
|---------|-------|----------|
| Color Space | Linear | Edit → Project Settings → Player → Other Settings |
| Multithreaded Rendering | ☑ | Edit → Project Settings → Player |
| Minimum API Level | 29 | Edit → Project Settings → Player → Android |
| Target API Level | 34 | Edit → Project Settings → Player → Android |
| Scripting Define Symbols | `CUESTRIKE_NORMCORE` (if Normcore installed) | Edit → Project Settings → Player |
| Strip Engine Code | ☑ | Edit → Project Settings → Player |
| Managed Stripping Level | Medium | Edit → Project Settings → Player |
| Install Time Texture Compression | ASTC | Edit → Project Settings → Player → Android |
| VR Supported | ☑ | Edit → Project Settings → Player → XR Settings |

### XR Plug-in Management

| Plug-in | Status |
|---------|--------|
| Oculus XR Plugin | ☐ Installed and configured |
| OpenXR | ☐ Installed and configured |
| Interaction Profile (Meta Quest) | ☐ Active |
| Feature Groups | ☐ Hand Tracking, Eye Gaze (if using) |

### Required Packages (Package Manager)

| Package | Version | Status |
|---------|---------|--------|
| XR Interaction Toolkit | 3.x+ | ☐ |
| XR Hands | 3.x+ | ☐ (if hand tracking) |
| XR Core Utilities | Latest | ☐ |
| OpenXR Plugin | Latest | ☐ |
| Oculus XR Plugin | Latest | ☐ |
| TextMesh Pro | Latest | ☐ |
| Universal RP | Compatible | ☐ |

### Performance Targets (Frame Debugger / Profiler)

| Metric | Target | Check |
|--------|--------|-------|
| Draw Calls | < 200 | ☐ |
| Triangles | < 200k | ☐ |
| SetPass Calls | < 50 | ☐ |
| Memory Usage | < 2 GB (Quest 2), < 4 GB (Quest 3) | ☐ |
| CPU Time (game thread) | < 11ms | ☐ |
| GPU Time | < 11ms | ☐ |

---

## 🎨 3. Store Assets & Media Requirements

### Required Uploads (Meta Quest Store Manager)

| Asset | Dimensions | Format | Status |
|-------|-----------|--------|--------|
| **App Video (Primary)** | 1920×1080 min, 1280×720 recommended | MP4 H.264 | ☐ |
| **App Video (Additional)** | Same specs | MP4 H.264 | ☐ |
| **Cover Art (Landscape)** | 2560×1440 | PNG/JPG | ☐ |
| **Cover Art (Portrait)** | 2160×1080 | PNG/JPG | ☐ |
| **Hero Banner** | 1080×360 | PNG/JPG | ☐ |
| **Screenshot (1)** | 1920×1080 min | PNG/JPG | ☐ |
| **Screenshot (2)** | Same | PNG/JPG | ☐ |
| **Screenshot (3)** | Same | PNG/JPG | ☐ |
| **Screenshot (4)** | Same | PNG/JPG | ☐ |
| **Store Icon** | 512×512 | PNG | ☐ |
| **Small Icon** | 128×128 | PNG | ☐ |
| **Feature Graphic** | 1024×500 | PNG (Google Play) | ☐ |

### Video Content Tips

- [ ] Show gameplay from first-person VR perspective
- [ ] Include both menu/navigation and active gameplay
- [ ] Demonstrate multiplayer (if applicable)
- [ ] Keep under 60 seconds (shorter is better)
- [ ] Use smooth camera — no sudden movements
- [ ] Add text overlays for key features
- [ ] Audio: clear voiceover or fitting music

### Screenshot Tips

- [ ] Capture from headset rendering (not editor)
- [ ] Show realistic in-game action
- [ ] Various angles: table overview, aiming, pocket view
- [ ] No UI clutter — clean screenshots
- [ ] Bright, appealing visuals
- [ ] Must not contain placeholder/developer text

---

## 📝 4. Store Listing Information

### Basic Info

| Field | Content |
|-------|---------|
| **App Name** | CueStrike |
| **Short Description** (≤ 50 chars) | Realistic VR pool & billiards simulation |
| **Long Description** (≤ 200 chars) | Step up to the table in CueStrike — a fully immersive VR pool simulation. Play 8-Ball, 9-Ball, Chinese Pool, and Noir modes with realistic physics and stunning visuals. Solo vs AI or cross-platform multiplayer. |
| **Category** | Sports / Simulation |
| **Genre** | Pool, Billiards, VR Sports |
| **Developer Name** | CueStrike Studios |
| **Support Email** | support@cuestrike.com |
| **Website** | https://cuestrike.com |

### Feature List (Bullet Points)

- [ ] Realistic physics simulation — authentic ball and cue behavior
- [ ] Multiple game modes: 8-Ball, 9-Ball, Chinese Pool, Noir Memory
- [ ] AI opponents with 4 difficulty levels (Easy to Expert)
- [ ] Cross-platform multiplayer via Normcore (up to 4 players)
- [ ] Hand tracking support for intuitive cue control
- [ ] Customizable table and environment themes
- [ ] Career mode with progression and challenges
- [ ] Practice mode — hone your skills alone
- [ ] Leaderboards and stats tracking

---

## 🔒 5. Permissions & Privacy

| Permission | Required | Purpose |
|-----------|----------|---------|
| Body Tracking | ☑ | For full-body immersion and cue handling |
| Hand Tracking | ☑ | (If using hand tracking mode) |
| Spatial Data | ☑ | Required for Meta Quest platform |
| Internet | ☑ | Multiplayer functionality (Normcore) |
| Microphone | ☐ | (Future voice chat) |

### Privacy Policy

- [ ] Privacy Policy URL created and hosted
- [ ] Covers: data collected (none/minimal), third-party services (Normcore), analytics
- [ ] GDPR compliant (if EU users)
- [ ] COPPA compliant (if under 13 users — unlikely for pool)

---

## 📋 6. IARC Age Rating

Complete the IARC questionnaire at: https://www.globalratings.com/

| Likely Ratings | Region |
|---------------|--------|
| **PEGI 3** | Europe |
| **ESRB E (Everyone)** | US/Canada |
| **USK 0** | Germany |
| **IARC 3+** | General |

### Content Descriptors (likely none needed)

- [ ] No violence (pool simulation)
- [ ] No sexual content
- [ ] No gambling/casino mechanics
- [ ] No purchase/pay-to-win (unless implementing)
- [ ] No user-generated content

---

## 👥 7. Multiplayer Requirements (Normcore)

| Requirement | Status |
|------------|--------|
| Normcore SDK installed & configured | ☐ |
| Normcore App Key active (dashboard) | ☐ |
| Normcore Terms of Service compliance | ☐ |
| Room system implemented | ☐ |
| Host migration (if host disconnects) | ☐ (Optional) |
| Anti-cheat: basic validation | ☐ |
| Matchmaking UI / quick join | ☐ |
| Player name validation | ☐ |
| Max player limit enforced | ☐ (4 players) |

### Normcore SDK Setup Steps

```
1. Install SDK: Window → Package Manager → + → Add package by name → com.normal.realtime
2. Get App Key: https://normcore.io → Dashboard → Create new app
3. Add CUESTRIKE_NORMCORE to Scripting Define Symbols
4. Attach Realtime component to CueStrikeNormcoreManager GameObject
5. Attach RealtimeView + RealtimeTransform to networked prefabs
6. Test with 2 builds (or 2 devices)
```

---

## 🧪 8. Device Testing Checklist

### Prerequisites

- [ ] APK built with correct settings (ARM64, IL2CPP, ASTC)
- [ ] Developer mode enabled on headset
- [ ] SideQuest or ADB connected
- [ ] Normcore app key active (for multiplayer testing)

### Testing on Device

| Test | Method | Result |
|-----|--------|--------|
| **Install APK** | `adb install CueStrike.apk` | ☐ Pass / ☐ Fail |
| **Launch** | App opens without crash | ☐ Pass / ☐ Fail |
| **Controller Pairing** | Controllers tracked in VR | ☐ Pass / ☐ Fail |
| **Menu Navigation** | All menu buttons work | ☐ Pass / ☐ Fail |
| **Game Start** | New game starts correctly | ☐ Pass / ☐ Fail |
| **Shooting** | Cue stick control works | ☐ Pass / ☐ Fail |
| **Physics** | Ball collision, pocketing works | ☐ Pass / ☐ Fail |
| **Foul Detection** | Fouls detected and called | ☐ Pass / ☐ Fail |
| **Game Modes** | All modes accessible | ☐ Pass / ☐ Fail |
| **AI Opponent** | AI takes turns, difficulty affects play | ☐ Pass / ☐ Fail |
| **Multiplayer** | Room creation, joining, sync (if tested) | ☐ Pass / ☐ Fail |
| **Scoring** | Scores update correctly | ☐ Pass / ☐ Fail |
| **End of Frame** | Frame completes, new frame starts | ☐ Pass / ☐ Fail |
| **End of Match** | Match completes, results screen | ☐ Pass / ☐ Fail |
| **Performance** | Stable 72 FPS after 10 min | ☐ Pass / ☐ Fail |
| **Crash Test** | No crashes after 30 min continuous play | ☐ Pass / ☐ Fail |

### Performance Profiling (on device)

- [ ] Use Oculus Performance HUD or `adb shell dumpsys gfxinfo`
- [ ] CPU: < 11ms per frame (for 72 FPS)
- [ ] GPU: < 11ms per frame
- [ ] Memory: < 1.5 GB for Quest 2, < 3 GB for Quest 3
- [ ] Temperature: Stable after 30 min

### Guardian Boundary

- [ ] App responds to guardian boundary crossing
- [ ] Passthrough mode works (if implemented)
- [ ] Recenter button works

---

## ✅ 9. Submission Steps (Meta Quest Store Manager)

### Step-by-Step Process

| Step | Description | Status |
|-----|-------------|--------|
| **1** | Create developer account at dashboard.oculus.com | ☐ |
| **2** | Set up organization (developer name, tax info) | ☐ |
| **3** | Create new app → select "Quest" platform | ☐ |
| **4** | Choose distribution: **App Lab** (public link) or **Quest Store** (full review) | ☐ |
| **5** | Upload APK (binary section) — must be at least version 1 | ☐ |
| **6** | Fill in store listing: name, description, category | ☐ |
| **7** | Upload store assets: video, screenshots, icons | ☐ |
| **8** | Complete IARC age rating questionnaire | ☐ |
| **9** | Configure permissions and privacy policy | ☐ |
| **10** | Set up release channels (Alpha → Beta → Release) | ☐ |
| **11** | Submit for review (full store) or publish (App Lab) | ☐ |

### App Lab vs Full Store

| Criteria | App Lab | Full Store |
|---------|---------|------------|
| **Review Process** | Automated (basic checks) | Full human + technical review |
| **Time to Publish** | 1-3 days | 2-8 weeks |
| **Visibility** | Only via direct link (no browsing) | Store search & discovery |
| **URL Sharing** | Yes | Yes |
| **Ratings** | Yes | Yes |
| **Monetization** | Supported | Supported |
| **Recommended Path** | **Start with App Lab** | Upgrade later |

---

## 🚀 10. Pre-Submission Final Checks

- [ ] APK builds without errors
- [ ] All compile errors = 0
- [ ] No placeholder textures/models/audio
- [ ] Test on 2+ Quest devices
- [ ] Multiplayer works (or remove from description if not ready)
- [ ] App icon is correct size and looks good
- [ ] Screenshots captured from headset (not editor preview)
- [ ] Video shows real gameplay (not mockup)
- [ ] Description has no typos
- [ ] Privacy policy is accessible via URL
- [ ] App name is not trademarked (check)
- [ ] App Lab release channel configured
- [ ] Version number is >= 1.0
- [ ] Bundle version code is unique (increment each upload)
- [ ] APK is signed with release key

---

## 📚 11. Reference Links

| Resource | URL |
|----------|-----|
| Meta Quest Developer Center | https://developer.oculus.com |
| Quest Store Manager | https://dashboard.oculus.com |
| App Lab Documentation | https://developer.oculus.com/resources/app-lab |
| Store Assets Guidelines | https://developer.oculus.com/resources/store-assets |
| IARC Rating | https://www.globalratings.com |
| Normcore SDK | https://normcore.io |
| Submission Checklist (Meta) | https://developer.oculus.com/resources/submission-checklist |

---

> **Status:** 🟡 In Progress (check off items as they are completed)
>
> Last Updated: July 30, 2026