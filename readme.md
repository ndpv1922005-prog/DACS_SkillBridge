\# SkillBridge — Skill Exchange Platform



SkillBridge is a modern fullstack web application that connects students and teachers for online/offline skill learning sessions.



The platform acts as a trusted middle layer between both parties:



\* Teachers can create profiles and offer skills.

\* Students can search, chat, book sessions, and join video calls.

\* The system controls booking flow and simulated escrow payment flow to reduce scam risks.



\---



\# Project Goal



Build a realistic MVP marketplace platform with:



\* Authentication

\* Teacher discovery

\* Realtime chat

\* Video call

\* Booking system

\* Escrow-like payment flow

\* Dashboard management



This project focuses on:



\* clean architecture

\* realtime experience

\* modern UI/UX

\* business workflow

\* scalable backend structure



\---



\# Core Features



\## Authentication



\* Register / Login

\* JWT Authentication

\* Cookie / Session based current user

\* Protected routes

\* Role system:



&#x20; \* Student

&#x20; \* Teacher



\---



\## Teacher Marketplace



\* Teacher profile

\* Teacher search

\* Skill categories

\* Pricing per session

\* Online / Offline teaching mode

\* Teacher detail page



\---



\## Realtime Chat



\* 1-to-1 realtime messaging

\* SignalR websocket communication

\* Chat history persistence

\* Online status

\* Typing indicator (optional)



\---



\## Video Call



\* WebRTC based video call

\* Peer-to-peer communication

\* Camera + microphone support

\* Booking-gated access



Only confirmed + paid bookings can join calls.



\---



\## Booking System



Students can:



\* choose teacher

\* select time

\* create booking request



Teachers can:



\* confirm booking

\* reject booking



Booking lifecycle:



```text

Pending

Confirmed

Paid

InProgress

Completed

Cancelled

Disputed

Released

```



\---



\## Escrow Payment Simulation



This project uses a simulated escrow flow instead of real payment gateway integration.



Why:



\* focus on core product architecture

\* reduce integration complexity

\* keep business workflow realistic



Flow:



1\. Student books session

2\. Teacher confirms

3\. Student clicks "Pay"

4\. System holds payment

5\. Session happens

6\. Student confirms completion

7\. System releases payment to teacher



\---



\## Dashboard



\### Student Dashboard



\* upcoming sessions

\* booking history

\* payment status

\* chat access



\### Teacher Dashboard



\* incoming bookings

\* earnings overview

\* pending payouts

\* schedule management



\---



\# Tech Stack



\## Frontend



\* React / Next.js

\* TypeScript

\* TailwindCSS

\* Shadcn UI

\* Zustand / Context API

\* SignalR Client

\* WebRTC



\---



\## Backend



\* ASP.NET Core Web API

\* Entity Framework Core

\* SQL Server

\* SignalR

\* JWT Authentication



\---



\# UI/UX Design Direction



The UI must feel modern, clean, and production-like.



\## Design Style



\* Minimalist

\* Professional

\* Soft shadows

\* Rounded cards

\* Spacious layout

\* Smooth transitions



\---



\## Main Colors



\### Primary



```css

\#6366F1

```



\### Secondary



```css

\#8B5CF6

```



\### Background



```css

\#0F172A

```



\### Card



```css

\#1E293B

```



\### Accent



```css

\#22C55E

```



\---



\# UI Requirements



\## Global Rules



\* Consistent spacing

\* Responsive design

\* Modern typography

\* Smooth hover animations

\* Skeleton loading

\* Toast notifications

\* Empty states

\* Error states



\---



\## Teacher Cards



Must include:



\* avatar

\* name

\* skill

\* short bio

\* price/session

\* rating

\* online status



Card should:



\* hover elevate

\* animate smoothly

\* have strong CTA buttons



\---



\## Chat UI



Must feel similar to:



\* Messenger

\* Discord

\* Telegram



Requirements:



\* realtime updates

\* auto scroll

\* online indicator

\* modern message bubbles

\* timestamp

\* unread badge



\---



\## Video Call UI



Requirements:



\* fullscreen layout

\* responsive video grid

\* camera toggle

\* microphone toggle

\* end call button

\* connection status



\---



\## Dashboard UI



Dashboard should include:



\* stats cards

\* booking tables

\* earnings summary

\* upcoming sessions

\* sidebar navigation



Use:



\* charts

\* cards

\* badges

\* tabs



\---



\# Folder Structure



```text

/src

&#x20;├── api

&#x20;├── components

&#x20;├── features

&#x20;├── hooks

&#x20;├── layouts

&#x20;├── pages

&#x20;├── services

&#x20;├── store

&#x20;├── styles

&#x20;├── types

&#x20;└── utils

```



\---



\# Backend Structure



```text

/backend

&#x20;├── Controllers

&#x20;├── Services

&#x20;├── Repositories

&#x20;├── DTOs

&#x20;├── Models

&#x20;├── Hubs

&#x20;├── Middleware

&#x20;├── Data

&#x20;└── Helpers

```



\---



\# Database Entities



\## Users



```text

id

name

email

passwordHash

role

avatarUrl

bio

createdAt

```



\---



\## TeacherProfiles



```text

id

userId

skill

description

pricePerSession

teachingMode

rating

```



\---



\## Bookings



```text

id

studentId

teacherId

startTime

endTime

status

meetingRoomId

```



\---



\## Messages



```text

id

senderId

receiverId

content

createdAt

isRead

```



\---



\## Transactions



```text

id

bookingId

amount

type

status

createdAt

```



Transaction Types:



```text

Hold

Release

Refund

```



\---



\# Business Rules



\## Booking Rules



\* Teacher cannot book themselves

\* Booking time cannot overlap

\* Only confirmed bookings can be paid

\* Only paid bookings can join calls



\---



\## Call Rules



\* Student + teacher only

\* Booking must exist

\* Booking status must be Paid



\---



\## Payment Rules



\* Payment held by platform

\* Release after completion

\* Cancelled booking can refund



\---



\# Performance Requirements



\* Fast page load

\* Optimistic UI updates

\* Lazy loading

\* Pagination for chat/messages

\* Efficient SignalR connection handling



\---



\# Security Requirements



\* JWT validation

\* Role authorization

\* Secure API routes

\* Input validation

\* Prevent unauthorized call access

\* Prevent fake booking access



\---



\# Development Roadmap



\## Day 1



\* Setup project

\* Authentication

\* Database schema

\* Current user system



\---



\## Day 2



\* Teacher profile

\* Teacher search

\* Teacher detail page



\---



\## Day 3



\* Realtime chat

\* SignalR integration

\* Message persistence



\---



\## Day 4



\* Video call prototype

\* WebRTC connection



\---



\## Day 5



\* Booking system

\* Booking confirmation

\* Call access control



\---



\## Day 6



\* Dashboard

\* Integration

\* Bug fixing

\* Chat history

\* UI polish



\---



\## Day 7



\* Escrow payment simulation

\* Notifications

\* Online status

\* Final polish



\---



\# Important Development Notes



This project is NOT a simple CRUD application.



Priority order:



1\. Smooth user flow

2\. Realtime experience

3\. Clean UI/UX

4\. Stable architecture

5\. Business logic consistency



Focus on:



\* user experience

\* realistic workflow

\* clean component structure

\* maintainable codebase



\---



\# Final Goal



The final product should feel like:



\* a real startup MVP

\* modern SaaS platform

\* scalable marketplace application



Users should be able to:



\* discover teachers

\* communicate instantly

\* book sessions

\* join calls

\* complete learning sessions smoothly



The application must look polished, modern, and presentation-ready.



