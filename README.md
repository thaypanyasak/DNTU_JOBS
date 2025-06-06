# DNTU JOBS – Part-time Job Finder Website

DNTU JOBS is a fullstack web application designed to help students find part-time job opportunities posted by local employers. The system includes user roles such as Job Seeker, Employer, and Admin, with different dashboards and features for each.

## 🔧 Technologies Used

- **Frontend:** HTML, CSS, JavaScript  
- **Backend:** ASP.NET Core (MVC)  
- **Database:** SQL Server  
- **Authentication:** JWT  
- **Other:** File upload, real-time chat features, role-based access control

---

## 🌐 Main Pages & Features

### 🏠 Home
- Currently under development (no main functionality)

---

### 🔍 Find A Job (List Job)
- Displays all job posts that are approved by Admin
- Only jobs with approved edits are shown
- Search and filter features: _not yet completed_

---

### 📄 Job Detail
- Displays full job information
- **Favourite button**: adds job to user's favourites list
- **Apply button**: links to the job application form
- **Chat button**: opens a mini chat box to contact the employer

---

### 👤 User Profile (Job Seeker)
- Upload profile image
- Update social media links
- Edit personal information
- Track submitted job applications

---

### 💬 User Chat Room
- Send text messages, emojis, and images
- Preview and delete images before sending
- Send multiple files at once (PDF, Word, Excel)
- Real-time communication with employers

---

## 🏢 Employer Dashboard

### 1. Profile
- Edit employer profile information

### 2. Manage Jobs
- **Create job post**: Requires admin approval
- **Edit job post**: Edited posts await admin approval
- **Delete job post**: Deleted posts require admin confirmation

### 3. Applicant Management
- Approve or reject job applicants
- Download applicant CVs
- View full profile of each applicant

### 4. Chat Room
- Real-time messaging with job seekers
- Send text, emojis, images, and multiple files

---

## 🛠️ Admin Dashboard

### 1. Manage Users
- Create, update, and delete user accounts
- Lock/unlock accounts *(planned feature)*

### 2. Manage Job Categories
- Create, update, and delete job categories

### 3. Manage Job Posts
#### Index Page
- View job posts by each employer
- Track how many posts are waiting for approval

#### Detail View
- Approve or reject job posts
- View post content before approving

### 4. Manage Edited Job Posts
#### Index Page
- View which employers submitted job edits
- Approve or reject edited job posts

---

## 📁 Project Structure (MVC)
- Controllers/
- Models/
- Views/
- wwwroot/ (Static assets)
- Data/ (Database context and seeders)
- Services/ (Business logic)
- Utils/ (Helper functions)

---

## 📌 Status
- Core features for all roles completed  
- Chat & File Upload fully functional  
- Admin tools in active development  
- Search & Filter on job listing page: pending

---

## 🧑‍💻 Author
Developed by **Panyasak Khamkeuth (Thệ)** – DNTU Student, Information Technology Major

---

## 📎 License
This project is for educational purposes only.
