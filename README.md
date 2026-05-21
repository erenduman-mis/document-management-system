# ✈️ Aircraft Technical Document Management System

Aircraft Technical Document Management System is an ASP.NET Core MVC web application designed to manage aviation technical documents with role-based access control, version tracking, audit logging, and dashboard reporting.

The system focuses on controlled document handling for aircraft maintenance and quality processes such as AMM, Service Bulletin, Airworthiness Directive, and QA documents.

---

## 🚀 Features

### 🔐 Authentication
- Cookie-based login and logout
- SHA-256 password hashing
- Protected pages with authorization
- Login and logout activity logging

### 👥 Role-Based Authorization
The system supports four roles:

| Role | Permissions |
|---|---|
| Admin | Full access, user management, delete documents, audit logs |
| Quality | Change document status, upload documents |
| Engineer | Upload documents and new versions |
| Viewer | View documents only |

### 📄 Document Management
- Upload PDF, DOCX, and XLSX files
- Store document metadata:
  - Title
  - Document type
  - Aircraft type
  - ATA Chapter
- Search and filter documents
- Pagination and sorting
- Soft delete support

### 🔄 Version Control
- Automatic version creation during upload
- Upload new versions for existing documents
- Previous versions are marked as obsolete
- Current version tracking
- File metadata storage:
  - File type
  - File size
  - SHA-256 hash
  - Upload date

### ✅ Document Workflow
Supported workflow:

```text
Draft → In Review → Approved / Rejected