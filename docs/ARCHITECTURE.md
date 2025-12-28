# AgroLink System Architecture

This document defines the high-level architecture, data design, and component interactions for the AgroLink system.

## Index

1. [System Context Diagram (C4 Level 1)](#1-system-context-diagram-c4-level-1)
2. [Container Architecture (C4 Level 2)](#2-container-architecture-c4-level-2)
3. [Component Design: Clean Architecture & CQRS](#3-component-design-clean-architecture--cqrs)
4. [Entity Relationship Diagram (ERD)](#4-entity-relationship-diagram-erd)
5. [Infrastructure / Deployment Diagram](#5-infrastructure--deployment-diagram)

## 1. System Context Diagram (C4 Level 1) 

This diagram represents the high-level context of the AgroLink system, showing how users interact with the system and its external dependencies.

```mermaid
graph TD
    User[User / Farmer]
    
    subgraph AgroLink_System [AgroLink System]
        API[AgroLink API]
    end
    
    subgraph External_Systems [External Infrastructure]
        Auth[Identity Provider / JWT]
        S3["Object Storage\n(AWS S3 / MinIO)"]
        Email[Email Service]
    end

    User -->|Manage Farms, Animals, Checklists| API
    API -->|Stores Photos| S3
    API -->|Authenticates| Auth
    API -->|Sends Notifications| Email
```

## 2. Container Architecture (C4 Level 2)

This diagram shows the high-level technology choices and how containers interact within the local/production environment.

```mermaid
graph TB
    Client["Client App\n(Web / Mobile)"]
    
    subgraph Docker_Host [Container Environment]
        API["AgroLink API\n(.NET 8)"]
        DB[("PostgreSQL\nDatabase")]
        MinIO[("MinIO / S3\nObject Storage")]
    end

    Client -->|HTTPS / JSON| API
    API -->|EF Core / SQL| DB
    API -->|AWS SDK| MinIO
    
    style API fill:#512bd4,stroke:#333,stroke-width:2px,color:white
    style DB fill:#336791,stroke:#333,stroke-width:2px,color:white
    style MinIO fill:#c72c48,stroke:#333,stroke-width:2px,color:white
```

## 3. Component Design: Clean Architecture & CQRS

The application follows strict Clean Architecture principles with a CQRS (Command Query Responsibility Segregation) pattern using MediatR.

```mermaid
flowchart LR
    subgraph Presentation [API Layer]
        Controller[Controllers]
    end

    subgraph Application [Application Layer]
        DTO[DTOs]
        Mediator[MediatR Pipeline]
        Validation[Behaviors / Validators]
        Handler[Command/Query Handlers]
        Interface[Interfaces]
    end

    subgraph Domain [Domain Layer]
        Entity[Entities]
        ValueObj[Value Objects]
        DomainEvent[Domain Events]
    end

    subgraph Infrastructure [Infrastructure Layer]
        RepoImpl[Repository Implementation]
        S3Impl[AWS S3 Service]
        DbContext[AgroLink DbContext]
    end

    Controller -->|Send Command/Query| Mediator
    Mediator -->|Pipeline| Validation
    Validation --> Handler
    Handler -->|Uses| Interface
    Handler -->|Manipulates| Entity
    
    RepoImpl -. Implements .-> Interface
    S3Impl -. Implements .-> Interface
    
    RepoImpl -->|Uses| DbContext
    DbContext -->|Maps| Entity
```

## 4. Entity Relationship Diagram (ERD)

Derived from `AgroLinkDbContext` and Domain Entities.

```mermaid
erDiagram
    FARM ||--|{ PADDOCK : contains
    PADDOCK ||--|{ LOT : contains
    LOT ||--|{ ANIMAL : contains
    
    ANIMAL ||--o{ ANIMAL : "mother of"
    ANIMAL ||--o{ ANIMAL : "father of"
    
    ANIMAL ||--|{ ANIMAL_OWNER : "owned via"
    OWNER ||--|{ ANIMAL_OWNER : "owns"
    
    ANIMAL ||--o{ MOVEMENT : "subject of"
    USER ||--o{ MOVEMENT : "registers"
    
    ANIMAL ||--o{ CHECKLIST_ITEM : "checked in"
    CHECKLIST ||--|{ CHECKLIST_ITEM : contains
    USER ||--o{ CHECKLIST : performs
    
    ANIMAL ||--o{ PHOTO : "has"
    FARM ||--o{ PHOTO : "has"
    PADDOCK ||--o{ PHOTO : "has"
    
    FARM {
        uuid Id
        string Name
        string Location
    }
    
    ANIMAL {
        uuid Id
        string Tag
        string Name
        string Sex
        string Breed
    }
    
    MOVEMENT {
        uuid Id
        string EntityType
        string Reason
        datetime Date
    }
    
    CHECKLIST {
        uuid Id
        string ScopeType
        datetime Date
        string Notes
    }
    
    PHOTO {
        uuid Id
        string UriLocal
        string UriRemote
        string EntityType
    }
```

## 5. Infrastructure / Deployment Diagram

This diagram visualizes the actual AWS resources and their connectivity, serving as the blueprint for Terraform scripts. It illustrates the network isolation and security boundaries.

```mermaid
flowchart TB
    User((User))
    
    subgraph AWS_Cloud [AWS Cloud Region]
        CF["CloudFront (CDN)"]
        WAF["AWS WAF (Security)"]
        S3[("S3 Bucket\n(Media Storage)")]
        
        subgraph VPC ["VPC (10.0.0.0/16)"]
            subgraph Public_Subnets ["Public Subnets (DMZ)"]
                APIGW["API Gateway"]
            end
            
            subgraph Private_Subnets ["Private Subnets (App & Data)"]
                Lambda["Lambda Function\n(.NET 8 API)"]
                RDS[("RDS Aurora/Postgres\n(Database)")]
                Secret["Secrets Manager"]
            end
            
            subgraph Security_Groups ["Security Groups"]
                SGLambda["SG: Lambda\n(Egress to RDS:5432)"]
                SGRDS["SG: RDS\n(Ingress from SGLambda)"]
            end
        end
        
        IAM["IAM Roles & Policies"]
    end
    
    User -->|HTTPS| CF
    CF --> WAF
    WAF --> APIGW
    APIGW -->|Trigger| Lambda
    
    Lambda -->|SQL| RDS
    Lambda -->|Read/Write| S3
    Lambda -->|Retrieve| Secret
    
    IAM -.->|Grants Permissions| Lambda
    
    %% Relationships to Security Groups
    Lambda -.-> SGLambda
    RDS -.-> SGRDS
```

### Key Infrastructure Components:

- **VPC Structure:** Highly available multi-AZ setup with Public Subnets for ingress (API Gateway) and Private Subnets for computing (Lambda) and data (RDS).
- **Security Groups:** 
    - `SGLambda`: Restricts outbound traffic only to required services (RDS, S3 endpoint).
    - `SGRDS`: Only allows inbound traffic on port 5432 from the Lambda's Security Group.
- **Identity & Access:** IAM Roles use the principle of least privilege, granting the Lambda function access only to specific S3 buckets and Secrets Manager keys.
- **Edge Security:** CloudFront combined with AWS WAF provides DDoS protection and global content delivery for static assets or API endpoints.