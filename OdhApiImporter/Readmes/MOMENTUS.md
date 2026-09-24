<!--
SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>

SPDX-License-Identifier: CC0-1.0
-->

# Momentus Import

Momentus is the venue and room booking system used at NOI and Eurac. This import
brings the venues, rooms and events booked in Momentus into the Open Data Hub as
`VenueV2` and `Event` records, so they show up in the API and on public displays
like the NOI totems and the Eurac video walls.

There are two syncs: **Venues** (rooms) and **Events**. Venues should run first,
because the event import needs to know which room belongs to which venue.

## Venue sync

Endpoint: `GET MOMENTUS/Venue/Update`

Reads all rooms from Momentus and creates or updates one venue per Momentus
venue, with one `RoomDetail` entry per room. Each room keeps a reference back to
its Momentus room id, which the event import later uses to figure out which
venue an event's rooms belong to.

## Event sync

Endpoint: `GET MOMENTUS/Event/Update`

For every event booked in Momentus for roughly the next year:

1. The event's booking data, its "functions" (room- and date-specific extra
   info) and its booked rooms are fetched from Momentus.
2. The venue that owns those rooms is looked up in the Open Data Hub.
3. Everything is combined into one Open Data Hub event, with one **EventDate**
   entry per booked day/room.
4. Events that used to come from Momentus but are no longer returned by the
   interface are set to inactive and unpublished, instead of being deleted.

Only rooms booked with usage type "event" are considered. Rooms booked for
setup, teardown, catering, etc. are ignored.

### Where a room can be published

Momentus rooms are tagged in the room name with a usage label, for example
`PUBLIC`, `VIDEOWALL`, `ROOM` or `PRIVATE`. This label decides on which public
displays/channels an event, and each of its individual dates/rooms, may appear:

| Room label | Published on (Eurac) | Published on (NOI) |
|---|---|---|
| PUBLIC | Eurac video wall + Eurac seminar room screen | NOI totem + today.noi.bz.it |
| VIDEOWALL | Eurac video wall | today.noi.bz.it |
| ROOM | Eurac seminar room screen | NOI totem |
| PRIVATE / unrecognised | not published | not published |

This is calculated twice:

- **For the whole event**, looking at all its booked rooms together, to fill the
  event's own "published on" list.
- **For each individual EventDate**, looking only at that room's label, to fill
  that EventDate's own "published on" list.

This means an event can be published overall, while one specific day/room
inside it (e.g. a private side-room booked for the same event) is not shown on
the public displays, or vice versa. An EventDate without any label keeps
working as before and is never hidden by this logic — the per-room "published
on" only takes effect once a room has an explicit label.

If the event is set to inactive, all "published on" entries are removed, on
the event and on every EventDate.

### Title, subtitle and description

Momentus has no dedicated title/subtitle fields on the event itself. Instead,
whoever plans the event in Momentus adds "functions" (extra items attached to
the booking) named `EN Title`, `DE Title`, `IT Title`, `EN SUBtitle`,
`DE SUBtitle` and `IT SUBtitle`. The text typed into that function's own
"Name" field becomes the title/subtitle in the given language.

- A function only counts for the **event's own** title/subtitle if it applies
  to the whole event (no specific room attached).
- If a function with the same purpose is attached to one specific room and
  date/time instead, its text becomes the **subtitle of that one EventDate**
  (e.g. a specific session that has its own subtitle, inside a multi-day/
  multi-room event).
- An event without at least one title in any language is skipped entirely and
  not imported.
- The event's description (`BaseText`) comes from the event's own Description
  field in Momentus, and is only filled in for languages that already got a
  title/subtitle from a function. It is never overwritten once set, so manual
  edits made afterwards in the Open Data Hub are preserved on the next import.

## Where things are kept

- Import logic (fetching from Momentus, building the Open Data Hub event/venue):
  [MOMENTUS/Parser/ParseMomentusData.cs](../../MOMENTUS/Parser/ParseMomentusData.cs)
- Momentus API client: [MOMENTUS/GetDataFromMomentus.cs](../../MOMENTUS/GetDataFromMomentus.cs)
- Import scheduling/orchestration:
  [OdhApiImporter/Helpers/MOMENTUS/MomentusEventsImportHelper.cs](../Helpers/MOMENTUS/MomentusEventsImportHelper.cs),
  [OdhApiImporter/Helpers/MOMENTUS/MomentusVenuesImportHelper.cs](../Helpers/MOMENTUS/MomentusVenuesImportHelper.cs)
- HTTP endpoints that trigger a sync:
  [OdhApiImporter/Controllers/UpdateApiController.cs](../Controllers/UpdateApiController.cs)
  (search for "MOMENTUS DATA SYNC")
