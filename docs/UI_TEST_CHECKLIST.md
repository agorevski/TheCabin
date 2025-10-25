# UI/UX Testing Checklist - The Cabin MAUI App

## Testing Overview

**Purpose:** Systematic verification of all UI elements and user interactions  
**Target Platform:** Android (net9.0-android)  
**Screens Covered:** 7 main screens  
**Total Test Cases:** 150+

---

## How to Use This Checklist

1. **Test Each Item Systematically** - Go through each section in order
2. **Mark Status** - Use ✅ Pass, ❌ Fail, ⏭️ Skipped, ⏸️ Blocked
3. **Record Bug IDs** - Link failures to bug tracking template
4. **Take Screenshots** - Capture failures for documentation
5. **Retest After Fixes** - Verify fixes don't cause regressions

### Status Legend
- ✅ **Pass** - Works as expected
- ❌ **Fail** - Does not work, bug filed
- ⏭️ **Skipped** - Not tested yet
- ⏸️ **Blocked** - Cannot test due to dependency
- 🔄 **Retest** - Fixed, needs verification

---

## Screen 1: MainPage (Primary Game Screen)

### 1.1 Page Load & Initialization
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| M-01 | Page loads without crash | ⏭️ | | |
| M-02 | ViewModel is bound correctly | ⏭️ | | |
| M-03 | Stats bar displays | ⏭️ | | |
| M-04 | Story feed is visible | ⏭️ | | |
| M-05 | Voice button renders | ⏭️ | | |
| M-06 | Bottom navigation displays | ⏭️ | | |

### 1.2 Stats Bar
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| M-10 | Location name displays | ⏭️ | | |
| M-11 | Health value shows correctly | ⏭️ | | |
| M-12 | Heart emoji (❤️) visible | ⏭️ | | |
| M-13 | Light level displays | ⏭️ | | |
| M-14 | Bulb emoji (💡) visible | ⏭️ | | |
| M-15 | Game time displays | ⏭️ | | |
| M-16 | Clock emoji (🕐) visible | ⏭️ | | |
| M-17 | Stats update after command | ⏭️ | | |

### 1.3 Story Feed
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| M-20 | CollectionView displays | ⏭️ | | |
| M-21 | Initial description shows | ⏭️ | | |
| M-22 | Scrolling works | ⏭️ | | |
| M-23 | Player commands appear | ⏭️ | | |
| M-24 | System messages appear | ⏭️ | | |
| M-25 | Success messages (green) | ⏭️ | | |
| M-26 | Failure messages (red) | ⏭️ | | |
| M-27 | Text wraps correctly | ⏭️ | | |
| M-28 | Auto-scrolls to bottom | ⏭️ | | |
| M-29 | Handles 100+ entries | ⏭️ | | |

### 1.4 Voice Control Button
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| M-30 | Button renders (80x80) | ⏭️ | | |
| M-31 | Circular shape correct | ⏭️ | | |
| M-32 | Microphone icon visible | ⏭️ | | |
| M-33 | Tap registers | ⏭️ | | |
| M-34 | Color changes when recording | ⏭️ | | |
| M-35 | Returns to idle color | ⏭️ | | |
| M-36 | Activity indicator shows | ⏭️ | | |
| M-37 | Button disabled when processing | ⏭️ | | |

### 1.5 Transcript Preview
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| M-40 | Hidden by default | ⏭️ | | |
| M-41 | Appears when listening | ⏭️ | | |
| M-42 | Shows "Listening..." | ⏭️ | | |
| M-43 | Shows recognized text | ⏭️ | | |
| M-44 | Hides after processing | ⏭️ | | |

### 1.6 Bottom Navigation
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| M-50 | All 7 buttons visible | ⏭️ | | |
| M-51 | Story Selector (📚) works | ⏭️ | | |
| M-52 | Save (💾) works | ⏭️ | | |
| M-53 | Load (📂) works | ⏭️ | | |
| M-54 | Inventory (🎒) works | ⏭️ | | |
| M-55 | Help (❓) works | ⏭️ | | |
| M-56 | New Game (🔄) works | ⏭️ | | |
| M-57 | TTS Toggle (🔊/🔇) works | ⏭️ | | |
| M-58 | Icons are clear/visible | ⏭️ | | |

---

## Screen 2: StoryPackSelectorPage

### 2.1 Page Load
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| S-01 | Page loads from MainPage | ⏭️ | | |
| S-02 | Header displays correctly | ⏭️ | | |
| S-03 | All 5 packs load | ⏭️ | | |
| S-04 | ScrollView works | ⏭️ | | |

### 2.2 Story Pack Cards
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| S-10 | Classic Horror card shows | ⏭️ | | |
| S-11 | Arctic Survival card shows | ⏭️ | | |
| S-12 | Fantasy Magic card shows | ⏭️ | | |
| S-13 | Sci-Fi Isolation card shows | ⏭️ | | |
| S-14 | Cozy Mystery card shows | ⏭️ | | |
| S-15 | Icons display (🏚️🗻🏰🚀🏔️) | ⏭️ | | |
| S-16 | Theme names visible | ⏭️ | | |
| S-17 | Descriptions readable | ⏭️ | | |
| S-18 | Difficulty badges show | ⏭️ | | |
| S-19 | Difficulty colors correct | ⏭️ | | |
| S-20 | Play time displays | ⏭️ | | |
| S-21 | Tags display (if present) | ⏭️ | | |

### 2.3 Interactions
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| S-30 | Tapping card selects pack | ⏭️ | | |
| S-31 | Selection navigates back | ⏭️ | | |
| S-32 | Cancel button works | ⏭️ | | |
| S-33 | Selection passed to MainPage | ⏭️ | | |

---

## Screen 3: InventoryPage

### 3.1 Page Load
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| I-01 | Page loads from MainPage | ⏭️ | | |
| I-02 | Header displays | ⏭️ | | |
| I-03 | Weight display shows | ⏭️ | | |
| I-04 | Items load from game state | ⏭️ | | |

### 3.2 Empty State
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| I-10 | Empty state shows when no items | ⏭️ | | |
| I-11 | Backpack emoji visible | ⏭️ | | |
| I-12 | Message is clear | ⏭️ | | |

### 3.3 Item Display
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| I-20 | Items render in CollectionView | ⏭️ | | |
| I-21 | Item icons/emojis show | ⏭️ | | |
| I-22 | Item names display | ⏭️ | | |
| I-23 | Item descriptions show | ⏭️ | | |
| I-24 | Item weights display | ⏭️ | | |
| I-25 | Cards are properly styled | ⏭️ | | |
| I-26 | Scrolling works with many items | ⏭️ | | |

### 3.4 Swipe Actions
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| I-30 | Swipe right reveals actions | ⏭️ | | |
| I-31 | Drop button visible | ⏭️ | | |
| I-32 | Use button visible | ⏭️ | | |
| I-33 | Drop button works | ⏭️ | | |
| I-34 | Use button works | ⏭️ | | |
| I-35 | Confirmation dialogs appear | ⏭️ | | |

### 3.5 Interactions
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| I-40 | Tap item shows details | ⏭️ | | |
| I-41 | Weight updates after drop | ⏭️ | | |
| I-42 | Close button works | ⏭️ | | |
| I-43 | Back navigation works | ⏭️ | | |

---

## Screen 4: SettingsPage

### 4.1 Page Load
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| SE-01 | Page loads from MainPage | ⏭️ | | |
| SE-02 | All sections visible | ⏭️ | | |
| SE-03 | Settings load from preferences | ⏭️ | | |
| SE-04 | ScrollView works | ⏭️ | | |

### 4.2 Voice Recognition Section
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| SE-10 | Section header displays | ⏭️ | | |
| SE-11 | Enable toggle works | ⏭️ | | |
| SE-12 | Push-to-talk toggle works | ⏭️ | | |
| SE-13 | Confidence slider renders | ⏭️ | | |
| SE-14 | Slider value updates | ⏭️ | | |
| SE-15 | Offline mode toggle works | ⏭️ | | |
| SE-16 | Test voice button present | ⏭️ | | |

### 4.3 TTS Section
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| SE-20 | Section header displays | ⏭️ | | |
| SE-21 | Enable narration toggle works | ⏭️ | | |
| SE-22 | Speech rate slider renders | ⏭️ | | |
| SE-23 | Speech rate updates | ⏭️ | | |
| SE-24 | Voice pitch slider renders | ⏭️ | | |
| SE-25 | Voice pitch updates | ⏭️ | | |
| SE-26 | Test TTS button present | ⏭️ | | |

### 4.4 Display Section
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| SE-30 | Section header displays | ⏭️ | | |
| SE-31 | Font size slider renders | ⏭️ | | |
| SE-32 | Font size updates | ⏭️ | | |
| SE-33 | Theme picker displays | ⏭️ | | |
| SE-34 | Theme options available | ⏭️ | | |
| SE-35 | Theme selection works | ⏭️ | | |

### 4.5 Actions & About
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| SE-40 | Reset button visible | ⏭️ | | |
| SE-41 | Reset confirmation appears | ⏭️ | | |
| SE-42 | Reset actually works | ⏭️ | | |
| SE-43 | About section displays | ⏭️ | | |
| SE-44 | Version number correct | ⏭️ | | |
| SE-45 | Copyright info present | ⏭️ | | |

### 4.6 Persistence
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| SE-50 | Settings save on change | ⏭️ | | |
| SE-51 | Settings persist after restart | ⏭️ | | |
| SE-52 | Close button works | ⏭️ | | |

---

## Screen 5: LoadGamePage

### 5.1 Page Load
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| L-01 | Page loads from MainPage | ⏭️ | | |
| L-02 | Header displays | ⏭️ | | |
| L-03 | Saved games load | ⏭️ | | |

### 5.2 Empty State
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| L-10 | Empty state when no saves | ⏭️ | | |
| L-11 | Message is helpful | ⏭️ | | |

### 5.3 Save Game Cards
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| L-20 | Cards render for each save | ⏭️ | | |
| L-21 | Save name displays | ⏭️ | | |
| L-22 | Theme name displays | ⏭️ | | |
| L-23 | Theme icon shows | ⏭️ | | |
| L-24 | Location displays | ⏭️ | | |
| L-25 | Health displays with color | ⏭️ | | |
| L-26 | Saved date displays | ⏭️ | | |
| L-27 | Play time displays | ⏭️ | | |
| L-28 | Cards properly styled | ⏭️ | | |

### 5.4 Interactions
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| L-30 | Tap card loads game | ⏭️ | | |
| L-31 | Load confirmation appears | ⏭️ | | |
| L-32 | Delete button (🗑️) visible | ⏭️ | | |
| L-33 | Delete confirmation appears | ⏭️ | | |
| L-34 | Delete actually removes save | ⏭️ | | |
| L-35 | Cancel button works | ⏭️ | | |
| L-36 | Navigation returns to MainPage | ⏭️ | | |
| L-37 | Game state loads correctly | ⏭️ | | |

---

## Screen 6: AchievementsPage

### 6.1 Page Load
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| A-01 | Page loads successfully | ⏭️ | | |
| A-02 | Header displays | ⏭️ | | |
| A-03 | Stats display | ⏭️ | | |
| A-04 | Achievements load | ⏭️ | | |

### 6.2 Filter Buttons
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| A-10 | All button visible | ⏭️ | | |
| A-11 | Unlocked button visible | ⏭️ | | |
| A-12 | Locked button visible | ⏭️ | | |
| A-13 | All filter works | ⏭️ | | |
| A-14 | Unlocked filter works | ⏭️ | | |
| A-15 | Locked filter works | ⏭️ | | |
| A-16 | Active filter highlighted | ⏭️ | | |

### 6.3 Achievement Display
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| A-20 | Achievement cards render | ⏭️ | | |
| A-21 | Icons/emojis display | ⏭️ | | |
| A-22 | Achievement names show | ⏭️ | | |
| A-23 | Descriptions show | ⏭️ | | |
| A-24 | Unlock status correct | ⏭️ | | |
| A-25 | Locked achievements dimmed | ⏭️ | | |
| A-26 | Unlocked achievements bright | ⏭️ | | |
| A-27 | Lock icon (🔒) for locked | ⏭️ | | |
| A-28 | Check icon (✅) for unlocked | ⏭️ | | |
| A-29 | Unlock date shows | ⏭️ | | |
| A-30 | Progress bars display | ⏭️ | | |

### 6.4 Interactions
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| A-40 | Scrolling works | ⏭️ | | |
| A-41 | Tap shows details (future) | ⏭️ | | |
| A-42 | Close button works | ⏭️ | | |
| A-43 | Stats update correctly | ⏭️ | | |

---

## Screen 7: Help Dialog

### 7.1 Display
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| H-01 | Help opens from MainPage | ⏭️ | | |
| H-02 | Dialog displays properly | ⏭️ | | |
| H-03 | Content is readable | ⏭️ | | |
| H-04 | Commands listed clearly | ⏭️ | | |
| H-05 | Examples provided | ⏭️ | | |
| H-06 | Close button works | ⏭️ | | |

---

## Cross-Screen Tests

### Navigation
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| X-01 | MainPage → Inventory → Back | ⏭️ | | |
| X-02 | MainPage → Settings → Back | ⏭️ | | |
| X-03 | MainPage → Story Selector → Back | ⏭️ | | |
| X-04 | MainPage → Load Game → Back | ⏭️ | | |
| X-05 | MainPage → Achievements → Back | ⏭️ | | |
| X-06 | Back button always works | ⏭️ | | |
| X-07 | Android back gesture works | ⏭️ | | |

### Data Persistence
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| X-10 | Game state persists navigation | ⏭️ | | |
| X-11 | Settings persist app restart | ⏭️ | | |
| X-12 | Saves persist app restart | ⏭️ | | |
| X-13 | Achievements persist | ⏭️ | | |

### Performance
| ID | Test Case | Status | Bug ID | Notes |
|----|-----------|--------|--------|-------|
| X-20 | Page transitions smooth | ⏭️ | | |
| X-21 | No UI lag on interactions | ⏭️ | | |
| X-22 | ScrollViews perform well | ⏭️ | | |
| X-23 | Memory usage reasonable | ⏭️ | | |
| X-24 | Battery drain acceptable | ⏭️ | | |

---

## Test Summary

### Overall Statistics
- **Total Test Cases:** 150+
- **Passed:** 0
- **Failed:** 0
- **Skipped:** 150+
- **Blocked:** 0
- **Pass Rate:** 0%

### By Screen
| Screen | Total | Pass | Fail | Rate |
|--------|-------|------|------|------|
| MainPage | 38 | 0 | 0 | 0% |
| StoryPackSelector | 23 | 0 | 0 | 0% |
| Inventory | 24 | 0 | 0 | 0% |
| Settings | 26 | 0 | 0 | 0% |
| LoadGame | 18 | 0 | 0 | 0% |
| Achievements | 24 | 0 | 0 | 0% |
| Help | 6 | 0 | 0 | 0% |
| Cross-Screen | 14 | 0 | 0 | 0% |

### Critical Bugs
1. [List critical bugs here]

### High Priority Bugs
1. [List high priority bugs here]

---

## Notes

- Test on multiple devices/emulators if possible
- Test in portrait and landscape orientations
- Test with different font sizes/accessibility settings
- Test with slow network (for future online features)
- Document any workarounds discovered
- Update this checklist as new features are added

**Last Updated:** 2025-10-25
