# User Requirements: Booking System Search & Filtering

## Search & Filtering Capabilities
The user should be able to:
- Search for bookable resources.
- Filter results by (including but not limited to):
    - **Facilities**
    - **Resource type**
    - **Time period**
    - **Availability status** (free/booked)
    - **Specific weekdays** (multiple selectable)
    - **Business/activity areas** (multiple selectable)
    - **Geographical area**
    - **Custom attributes** (special properties)

## Display Preferences
- The user should have the option to **view results in a calendar overview**.
- The calendar must support **simultaneous display of multiple bookable objects**.  
## Resource Selection & Booking Flow (AK03)
The user must be able to:
- **Select one or multiple available resources** from search/filter results.
- **Proceed to the booking function** after selection.

### Key Details:
- ✅ **Multi-select support** (users can choose multiple resources in one session).
- 🔄 **Seamless transition** to booking (e.g., "Book Now" button activates after selection).
- 🚫 **Validation**: Disable booking if selected resources become unavailable during the process.

---
**Example Flow**:
1. User filters resources → Selects 2 conference rooms → Clicks "Book Selected".
2. System redirects to booking form with pre-filled resources.

# Booking System Requirements Specification

## User Roles and Permissions

### 1. Customer Users
**Authorized customer representatives can:**
- 📝 **Account Management**
    - Accept/acknowledge terms & conditions
    - Update own customer information
- 🔍 **Booking Overview**
    - View current and historical bookings
    - Act as contact for seasonal booking planning
- ✍️ **Booking Actions**
    - Book resources (see [AK04](#))
    - Cancel individual bookings (including recurring)
    - Cancel all bookings
    - Cancel all bookings at specific facility
- 🔔 **Notifications**
    - Receive status updates affecting bookings
- 💰 **Financial**
    - Make payments (see [AK05](#))
- 🏷️ **Operations**
    - Complete activities (see [AK06](#))

### 2. Group Leaders
**Group leaders can:**
- 👀 **Viewing Access**
    - View customer bookings
    - View group bookings
- ✍️ **Booking Actions**
    - Book ad-hoc slots (when authorized, see [AK04](#))
- 💰 **Financial**
    - Process payments (when authorized, see [AK05](#))
- 🏷️ **Operations**
    - Complete activities (see [AK06](#))

## System Requirements

### 3. Booking Terms & Conditions
- 🔐 **Mandatory Acceptance**
    - Customer administrators must accept current T&Cs
    - System must prevent bookings until T&Cs are accepted
    - Requires re-acceptance after updates

### 4. Calendar Integration
- 📅 **iCalendar Support**
    - Customer administrators can view bookings in personal calendar
    - Supports standard iCalendar (.ics) format
    - Read-only synchronization

## Reference Requirements
| ID    | Description                  |
|-------|------------------------------|
| AK04  | Resource Booking Function    |
| AK05  | Payment Processing           |
| AK06  | Activity Completion          |

## Technical Notes
- **Session Timeout**: 30 minutes inactivity
- **Data Retention**: Booking history kept for 5 years
- **Audit Log**: All booking changes recorded

# Booking System Functionality

## 1. Book Resources
**Authorized customer users can:**

### Terms & Access
- ☑️ **Conditionally accept** customer and booking terms (when required)

### Booking Types
- **Single/Multiple Resources**:
    - 🗓️ Book one or more resources for:
        - A single time period
        - Multiple specific occasions
        - Recurring occasions
- 🔄 **Recurring Booking Support**:
    - System assists with regular/time-based bookings

### Post-Booking
- 💳 **Payment processing** for bookings
- ✉️ **Receive confirmation** with check-in information
- ❌ **Cancel** one or multiple bookings

---

## 2. Join Waitlist
**Authorized customer users can:**

### Queue Functionality
- 🚶 **Join queue** for access to bookable resources
- 📝 **Specify preferences**:
    - Preferred facilities
    - Desired time slots
- ⚖️ **System consideration**:
    - Preferences should be factored in queue processing

---

### Related System Requirements
| ID    | Description                  |
|-------|------------------------------|
| AK04  | Core Booking Functionality   |
| AK05  | Payment Gateway Integration  |