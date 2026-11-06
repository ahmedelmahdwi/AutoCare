# 🚗 AutoCare Workshop Management System
### Clean Architecture Solution & Enterprise Refactoring

نظام إدارة مراكز صيانة السيارات والورش المتكامل (**AutoCare**) تم بناؤه وإعادة هيكلته معمارياً بنسبة 100% وفق أفضل ممارسات هندسة البرمجيات النظيفة (**Clean Architecture**) وعزل الطبقات الفيزيائي الصارم، مع الحفاظ الكامل على كافة قواعد العمل (Zero Feature Drift) والتصميم البصري الأصلي (Zero UI Drift).

---

## 🏗️ الهيكل المعماري للنظام (Architecture Overview)

يتكون الحل البرمجي من خمسة مشاريع منظمة في مسار تبعيات أحادي الاتجاه يعزل منطق العمل عن قواعد البيانات وواجهات المستخدم:

```
[ AutoCare.WPF (Presentation / Clean MVVM) ]
                 │
                 ▼
[ AutoCare.Infrastructure (Dapper / SQLite / Migrations) ]
                 │
                 ▼
[ AutoCare.Application (Use Cases / Interfaces / DTOs) ]
                 │
                 ▼
[ AutoCare.Domain (Pure Domain Entities / Value Objects / Rules) ]
```

### 📁 فهرس المجلدات والمشاريع (Directory Structure)

```
AutoCare/
├── AutoCare.sln                      # ملف الحل الشامل لـ Visual Studio / Rider
├── AutoCare.Domain/                  # طبقة النطاق النقية (Entities, Value Objects, Enums, Exceptions)
├── AutoCare.Application/             # طبقة التطبيق (Use Cases, Repository Interfaces, DTOs, IUnitOfWork)
├── AutoCare.Infrastructure/          # طبقة البنية التحتية (Dapper Repositories, Migrations, SQLite)
├── WPF_AutoCare/                     # طبقة العرض (Views XAML, Clean MVVM ViewModels, Theme, DI Container)
├── AutoCare.Tests/                   # حزمة الاختبارات الآلية الشاملة (274 اختبار آلي بنسبة نجاح 100%)
├── AutoCare_Distribution/            # حزمة التشغيل والتوزيع المستقلة المجمعة والجاهزة للإنتاج
├── docs/                             # وثائق التصميم المعماري وخطط التنفيذ
│   ├── reports/                      # تقارير التنفيذ المفصلة للمراحل (Phase 0 -> Phase 6)
│   ├── AutoCare_Architecture_Proposal.html / .pdf
│   └── AutoCare_Refactoring_Execution_Plan.html / .pdf
├── build_clean_solution.ps1          # سكربت التجميع الآلي الشامل للحل النظيف مع فحص النقاء
├── package_release.ps1               # سكربت فحص وتجميع حزمة الإصدار الرسمية ZIP
├── REFACTORING_PROGRESS.md           # سجل التتبع ومؤشرات إنجاز مراحل الهيكلة (100% مكتمل)
└── README.md                         # الدليل الإرشادي ومستند البداية السريعة
```

---

## 🛠️ أدوات التجميع والتشغيل السريع (Build & Run)

### 1. تجميع الحل البرمجي بالكامل:
```powershell
powershell -ExecutionPolicy Bypass -File .\build_clean_solution.ps1
```
يقوم هذا السكربت بتجميع كافة الطبقات بترتيب صارم (`Domain` -> `Application` -> `Infrastructure` -> `WPF`) ثم يجري فحص انعكاس تحليلي (Reflection Purity Check) للتأكد من خلو الطبقات الداخلية من أي تبعيات غير مسموح بها.

### 2. تشغيل الاختبارات الآلية (274 Test Cases):
```powershell
powershell -ExecutionPolicy Bypass -File .\AutoCare.Tests\run_tests.ps1
```
يقوم بفحص كافة وحدات النطاق، والتحقق من حالات الاستخدام، واختبار العزل المعماري، واختبار دورة حياة الورشة الكاملة E2E بنسبة نجاح 100%.

### 3. إنشاء حزمة التوزيع الرسمية (Production ZIP):
```powershell
powershell -ExecutionPolicy Bypass -File .\package_release.ps1
```
يقوم ببناء النظام، وتجهيز مجلد التوزيع [`AutoCare_Distribution`](file:///c:/Users/UEFI/Desktop/AutoCare_prjoct/FFF/C%23%20project_Auto_Care%20COMPLETE/AutoCare_Distribution)، وإنشاء ملف الأرشيف المضغوط الرسمي [`AutoCare_Clean_Architecture_Release.zip`](file:///c:/Users/UEFI/Desktop/AutoCare_prjoct/FFF/C%23%20project_Auto_Care%20COMPLETE/AutoCare_Clean_Architecture_Release.zip).

---

## 🛡️ الثوابت المحفوظة بدقة (Preserved Invariants)

1. **أرقام اللوحات (Plate Numbers):** 7 خانات مطابقة للمواصفات.
2. **أرقام الهياكل (VIN):** 17 خانة بالضبط ومحمية عبر كائنات القيمة.
3. **أرقام الهواتف:** 10 أرقام.
4. **المعادلات المالية:** نسبة الضريبة 15%، عمولات الفنيين، والتحقق الصارم من صحة الحسابات.
5. **محرك التفقيط العربي (Arabic Tafqeet):** تحويل المبالغ المالية إلى كلمات عربية بالريال والهللات بدقة.
6. **الباركود الضريبي ZATCA TLV:** التشفير المتوافق مع متطلبات هيئة الزكاة والضريبة والجمارك.
7. **خصم المخزون الذري:** حماية المخزون من البيع بالسالب مع القفل التفاؤلي (`RowVersion`).
8. **الحذف الآمن (Soft Delete):** الحفاظ على السجلات المحذوفة مع تفعيل `IsDeleted = 1`.
9. **التصميم الأصلي (Zero UI Drift):** الحفاظ الكامل على كافة الألوان والخطوط والمؤثرات الزجاجية.

---

## 📑 تقارير التنفيذ والتوثيق (Documentation & Reports)

- [سجل تتبع الهيكلة الشامل (REFACTORING_PROGRESS.md)](file:///c:/Users/UEFI/Desktop/AutoCare_prjoct/FFF/C%23%20project_Auto_Care%20COMPLETE/REFACTORING_PROGRESS.md)
- [المرحلة 0: خط الأساس وشبكة الأمان](file:///c:/Users/UEFI/Desktop/AutoCare_prjoct/FFF/C%23%20project_Auto_Care%20COMPLETE/docs/reports/PHASE_0_EXECUTION_REPORT.md)
- [المرحلة 1: العزل الفيزيائي وتدشين الطبقات](file:///c:/Users/UEFI/Desktop/AutoCare_prjoct/FFF/C%23%20project_Auto_Care%20COMPLETE/docs/reports/PHASE_1_EXECUTION_REPORT.md)
- [المرحلة 2: ترحيل طبقة النطاق وتطهيرها](file:///c:/Users/UEFI/Desktop/AutoCare_prjoct/FFF/C%23%20project_Auto_Care%20COMPLETE/docs/reports/PHASE_2_EXECUTION_REPORT.md)
- [المرحلة 3: طبقة التطبيق وحالات الاستخدام](file:///c:/Users/UEFI/Desktop/AutoCare_prjoct/FFF/C%23%20project_Auto_Care%20COMPLETE/docs/reports/PHASE_3_EXECUTION_REPORT.md)
- [المرحلة 4: البنية التحتية والبيانات ومحرك الترقية](file:///c:/Users/UEFI/Desktop/AutoCare_prjoct/FFF/C%23%20project_Auto_Care%20COMPLETE/docs/reports/PHASE_4_EXECUTION_REPORT.md)
- [المرحلة 5: نمط MVVM النظيف وتجريد الواجهات](file:///c:/Users/UEFI/Desktop/AutoCare_prjoct/FFF/C%23%20project_Auto_Care%20COMPLETE/docs/reports/PHASE_5_EXECUTION_REPORT.md)
- [المرحلة 6: الفحص التكاملي الشامل E2E وحزمة الإصدار](file:///c:/Users/UEFI/Desktop/AutoCare_prjoct/FFF/C%23%20project_Auto_Care%20COMPLETE/docs/reports/PHASE_6_EXECUTION_REPORT.md)
