window.consoleBg = (function () {
    'use strict';

    // ── Window title bar labels ─────────────────────────────────────────────
    var TITLES = [
        'proc/8812',  'proc/9144',  'proc/3301',  'proc/7712',  'proc/4492',
        'proc/1187',  'proc/6603',  'proc/2255',  'proc/5519',  'proc/8001',
        'bci_daemon', 'neural_rx',  'cortex_sync','grip_cls',   'bim_proc',
        'rbs_ctrl',   'hkb_mon',    'port_sel',   'slug_feed',  'buck_feed',
        'kern/sched', 'kern/vm',    'kern/ipc',   'kern/net',   'kern/fs',
        'kern/mm',    'kern/irq',   'kern/sig',   'kern/clock', 'kern/audit',
        'sys/netd',   'sys/logd',   'sys/cryptd', 'sys/authd',  'sys/watchd',
        'sys/arbiter','sys/broker', 'sys/relay',  'sys/mirror', 'sys/janitor',
        'net/tx_mon', 'net/rx_mon', 'net/gate',   'net/proxy',  'net/scanner',
        'net/sniffer','net/balancer','net/tunnel', 'net/beacon', 'net/resolver',
        'db/node_idx','db/edge_map','db/query',   'db/cache',   'db/repl',
        'db/compact', 'db/journal', 'db/txn_log', 'db/vacuum',  'db/snapshot',
        'sec/auditor','sec/policy', 'sec/token',  'sec/vault',  'sec/scanner',
        'sec/ids',    'sec/firewall','sec/probe',  'sec/enclave','sec/signer',
        'glmz/relay', 'glmz/gate',  'glmz/node',  'glmz/border','glmz/comms',
        'crest/rbs',  'crest/bim',  'crest/hkb',  'crest/led',  'crest/cal',
        'corp/enforcer','corp/monitor','corp/audit','corp/sync', 'corp/mirror',
        'dist7/node', 'dist12/hub', 'dist4/fab',  'dist9/relay','dist2/comms',
        'arc/compress','arc/index', 'arc/verify', 'arc/extract','arc/delta',
        'jit/compile', 'jit/cache', 'jit/evict',  'jit/patch',  'jit/trace',
    ];

    // ── Console output lines ────────────────────────────────────────────────
    var LINES = [
        // original 28
        '0x4f3a :: dispatch_sync[0x1b] → q',
        'pid:8812 ipc.recv buf=0xffc2 ok',
        'kern: mmap 0x7ffe3200 sz=4096 rw',
        'net.tx 172.16.44.12:5555 1400B',
        'sys.fork → child:9144 inherit_fd',
        'ring3 trap #14 cr2=0x00000008',
        'sched: ctx-swap 8812→9144 Δ=2μs',
        'fs.read /proc/9144/maps 4096B',
        'db.exec idx_scan nodes n=847',
        'alloc: 0x5582c4a0 64B heap ok',
        'lib: dlopen libneural.so.3 ok',
        'audit: cap_check euid=1000 NET',
        'rx:127.0.0.1:44312 frame 0xd4',
        'jit: compile fn@0x5582c4b8 ok',
        'sig: SIGUSR2 pid:8812 → queued',
        'bim: grip[Δ=+0.027] → slug',
        'rbs: rotate 38ms port_beta lock',
        'bci: cortex_rx lat=4ms handshake',
        'net.syn ACK 0x9f3c seq++',
        'vm: page_fault 0x800 recover',
        'cache: evict lru sz=256K ok',
        'ipc.gate:0x1b [enc payload fwd]',
        'sec: token_verify uid=0 ok',
        'crypt: aes256 blk=0x44 done',
        'tsk: worker:12 idle→run lat=1ms',
        'buf: flush 0x3c00 sz=8192B ok',
        'ev: POLLIN fd=7 consumed',
        'arc: compress ratio=2.4 ok',
        // 100 new entries
        'bci: motor_cortex signal lat=3ms',
        'bci: grip_tension[L]=0.41 stable',
        'bci: intent_cls → precision ok',
        'bci: calibrate epoch=7 Δ=0.003',
        'bci: pair sig 0xBF44 accepted',
        'bci: extensor_detect 12ms lock',
        'bci: sweep_cls → buck confirmed',
        'bci: neural_key 0xA3F7 valid',
        'bci: contact_ring[3] Δ=+0.014',
        'bci: 4hr_cal acc=97.3% commit',
        'rbs: disc idle → alpha port',
        'rbs: beta_lock acquired 38ms',
        'rbs: detent spring load=2.1N',
        'rbs: motor_cmd enc[0x22] ok',
        'rbs: yellow_led transit 38ms',
        'rbs: red_led port_alpha active',
        'rbs: manual_override lever→buck',
        'hkb: piston stroke 8mm recoil',
        'hkb: impulse 8ms→120ms spread',
        'hkb: perceived_kick -40% ok',
        'comp: port1 gas_vent upward ok',
        'comp: muzzle_flip correction ok',
        'port_alpha: round_count=2 ok',
        'port_beta: slug_seated fwd ok',
        'port_beta: brass_cap ejector ok',
        'led: pin_tactile protrude slug',
        'net.tx 10.44.7.3:8443 tls1.3',
        'net.rx 192.168.44.12 ack=0x3c',
        'net: retransmit seq=0x9f3d #2',
        'net: route 10.0.0.0/8 via gw4',
        'net: dns glmz.relay.7 → ok',
        'net: keepalive dist12 ok 44ms',
        'net: tls_handshake → resumed',
        'net: cipher AES-256-GCM ok',
        'net: drop 172.31.0.0 blacklist',
        'net: nat 10.44.7.3→203.0.113.9',
        'glmz: border_node[7] ping 12ms',
        'glmz: district12 relay online',
        'glmz: gate_check corp_key ok',
        'glmz: freelancer_id 0x3301 seen',
        'glmz: comm_node district4 ok',
        'glmz: enforcement_ping no reply',
        'glmz: dark_node 10.x seen 1x',
        'glmz: relay_hop 7→12 lat=18ms',
        'corp: sync_pulse ack 0x44 ok',
        'corp: audit_log appended row 8k',
        'corp: policy_check PASS uid=0',
        'corp: rekey_interval 3600s ok',
        'corp: enclave_attest sig valid',
        'corp: mirror_sync lag=44ms ok',
        'db: edge_insert n=3 commit ok',
        'db: node_update id=0x3301 ok',
        'db: idx_rebuild edges done',
        'db: page_split btree 0x4f ok',
        'db: wal_checkpoint frames=2048',
        'db: vacuum reclaim 12 pages',
        'db: query_plan cost=84 hash-join',
        'db: lock shared tbl:nodes ok',
        'db: txn_begin iso=read-committed',
        'db: txn_commit lsn=0x3af7 ok',
        'mem: brk 0x5582c800 +4096 ok',
        'mem: munmap 0x7f3a0000 ok',
        'mem: huge_page 2M mapped ok',
        'mem: oom_score pid:8812 adj=-8',
        'mem: slab reclaim 64 objects',
        'mem: rss_cur=148M rss_peak=201M',
        'mem: swap_in 4 pages 2ms',
        'mem: numa_balance migrate ok',
        'fs: open /var/log/bci.log rw',
        'fs: fsync /var/db/nodes.db ok',
        'fs: inode 8191 ref++ ok',
        'fs: dentry_cache hit 0x3301',
        'fs: dirty_pages flush 32 ok',
        'fs: ext4 blk_alloc 0x3af grp2',
        'fs: journal commit seq=0x1b ok',
        'crypt: chacha20 stream init ok',
        'crypt: hmac-sha256 verify ok',
        'crypt: ecdh p256 derive ok',
        'crypt: rng reseed RDRAND ok',
        'crypt: x25519 keyex done ok',
        'crypt: kdf pbkdf2 iter=100k ok',
        'sec: iptables ACCEPT dst:8443',
        'sec: uid=1000 cap drop NET_RAW',
        'sec: seccomp allow:read,write',
        'sec: ptrace denied pid:9144',
        'sec: selinux enforce domain ok',
        'sec: cert_verify depth=2 ok',
        'sec: crl_check revoked=false',
        'sec: vault_unseal shard 2/3 ok',
        'sched: load avg 0.82 0.74 0.61',
        'sched: preempt lat p99=88μs',
        'sched: cgroup cpu.shares=512',
        'sched: rr timeslice 5ms reset',
        'irq: softirq NET_RX 14 fired',
        'irq: tasklet bci_rx queued',
        'irq: affinity cpu2 set ok',
        'pci: dev 0:1b.0 dma_map ok',
        'usb: dev 3-1.2 xfer 512B ok',
        'arc: lz4 ratio=3.1 blk=64K',
        'arc: delta_encode base=0x3a ok',
        'arc: integrity sha1 match ok',
        'jit: trace loop@0x5582 hot',
        'jit: inline depth=3 ok',
        'jit: deopt guard miss patch',
        'ipc: shm_attach key=0x3301 ok',
        'ipc: mq_send qid=4 sz=64 ok',
        'ipc: sem_wait set=0 val→0',
    ];

    // ── Exit results ────────────────────────────────────────────────────────
    var OK_RESULTS = [
        'OK   exit:0  Φ',
        'OK   sync verified',
        'OK   handshake accepted',
        'OK   gate:open',
        'OK   0 faults',
        'OK   dispatch complete',
        'OK   checksum pass',
        'OK   commit 0x3af7',
        'OK   calibration saved',
        'OK   relay ack',
        'OK   token valid',
        'OK   neural lock',
        'OK   4 pages reclaimed',
        'OK   cert verified',
        'OK   latency within SLA',
    ];

    var ERR_RESULTS = [
        'ERR  timeout gate:0x1b',
        'ERR  seg fault cr2=null',
        'ERR  auth rejected',
        'ERR  ipc EOF unexpected',
        'ERR  ENOMEM alloc failed',
        'ERR  checksum mismatch',
        'ERR  sig lost pid:8812',
        'ERR  neural desync',
        'ERR  rbs jam detected',
        'ERR  relay no ack 3x',
        'ERR  cert revoked',
        'ERR  heap corrupted',
        'ERR  lock timeout 500ms',
        'ERR  bci pair lost',
        'ERR  port beta empty',
    ];

    // ── Fatal error dialog ──────────────────────────────────────────────────
    var FATAL_TITLES = [
        // original 7
        'FATAL — kernel panic',
        'FATAL — process crash',
        'CRITICAL — unhandled exception',
        'PANIC — memory violation',
        'FATAL — core dump',
        'CRITICAL — stack overflow',
        'FATAL — assertion failed',
        // 100 new entries
        'FATAL — bci_daemon abort',
        'FATAL — neural sync lost',
        'FATAL — rbs motor fault',
        'FATAL — grip classifier panic',
        'FATAL — bim chip unresponsive',
        'FATAL — port alpha jam',
        'FATAL — port beta misfire',
        'FATAL — hydraulic buffer fail',
        'FATAL — neural key revoked',
        'FATAL — cortex signal lost',
        'CRITICAL — heap corruption',
        'CRITICAL — use-after-free',
        'CRITICAL — buffer overflow',
        'CRITICAL — integer overflow',
        'CRITICAL — null dereference',
        'CRITICAL — race condition',
        'CRITICAL — deadlock detected',
        'CRITICAL — ipc gate closed',
        'CRITICAL — auth token expired',
        'CRITICAL — tls cert revoked',
        'CRITICAL — db corruption',
        'CRITICAL — wal log truncated',
        'CRITICAL — index corruption',
        'CRITICAL — disk full abort',
        'CRITICAL — fd limit exceeded',
        'CRITICAL — net partition',
        'CRITICAL — relay timeout 3x',
        'CRITICAL — glmz node offline',
        'CRITICAL — corp sync lost',
        'CRITICAL — enclave attest fail',
        'PANIC — double free',
        'PANIC — stack smash',
        'PANIC — wild pointer write',
        'PANIC — irq storm',
        'PANIC — vm corruption',
        'PANIC — slab underflow',
        'PANIC — dma overrun',
        'PANIC — watchdog timeout',
        'PANIC — nmi received',
        'PANIC — cpu lockup',
        'ABORT — assertion gate.cc',
        'ABORT — invariant violated',
        'ABORT — contract breach',
        'ABORT — precondition fail',
        'ABORT — postcondition fail',
        'ABORT — type confusion',
        'ABORT — format string fault',
        'ABORT — canary smashed',
        'ABORT — signal 11 SIGSEGV',
        'ABORT — signal 6 SIGABRT',
        'ABORT — signal 4 SIGILL',
        'ABORT — signal 8 SIGFPE',
        'ERROR — out of memory',
        'ERROR — device io fault',
        'ERROR — disk read error',
        'ERROR — journal replay fail',
        'ERROR — snapshot corrupt',
        'ERROR — replication lag',
        'ERROR — network unreachable',
        'ERROR — dns resolution fail',
        'ERROR — tls handshake fail',
        'ERROR — certificate expired',
        'KERNEL — divide by zero',
        'KERNEL — bad page table',
        'KERNEL — invalid opcode',
        'KERNEL — general protection',
        'KERNEL — page fault #14',
        'KERNEL — machine check',
        'KERNEL — oops in slab',
        'KERNEL — kasan report',
        'KERNEL — kcsan data race',
        'KERNEL — lockdep violation',
        'CREST — rbs disc shattered',
        'CREST — bim chip fused',
        'CREST — neural key tamper',
        'CREST — hkb piston seized',
        'CREST — battery critical 0%',
        'GLMZ — gate blacklist',
        'GLMZ — border lockdown',
        'GLMZ — district feed cut',
        'GLMZ — enforcement purge',
        'GLMZ — dark node exposure',
        'CORP — policy violation',
        'CORP — audit trail break',
        'CORP — enclave compromise',
        'CORP — zero-day triggered',
        'CORP — exfil detected',
        'BCI — motor signal flood',
        'BCI — calibration corrupt',
        'BCI — foreign key injection',
        'BCI — denial-of-sensation',
        'BCI — interface lockout',
        'SEC — intrusion confirmed',
        'SEC — privilege escalation',
        'SEC — lateral move detected',
        'SEC — exfil in progress',
        'SEC — rootkit signature',
    ];

    var FATAL_MSGS = [
        // original 7
        'NullPointerException at 0x5582c4b8\nThread 8812 terminated — backtrace dumped\nCore written to /tmp/core.8812',
        'Segmentation fault (core dumped)\nAddress 0x00000000 not mapped\nSignal 11 (SIGSEGV) at pid:9144',
        'EXCEPTION_ACCESS_VIOLATION 0xc0000005\nRead from 0x00000000000000a8\nModule: libneural.so.3 +0x4f3a',
        'Stack smashing detected\nAbort trap: 6\nProgram: bci_daemon pid:8812',
        'Assertion failed: ptr != nullptr\nFile: gate.cc line 1403\nAborting pid:8812',
        'Out of memory: kill process 8812\noom_score_adj=0 vm_rss=1.9G\nKernel panic — not syncing',
        'Double free detected in tcache 2\nCorrupted pointer: 0x5582c4a0\nAbort trap — heap integrity check',
        // 100 new entries
        'bci_daemon: neural handshake timeout 5000ms\nGrip classifier lost contact pid:8812\nBIM chip not responding — abort',
        'BIM accuracy drop: 61.2% (threshold 80%)\nCalibration epoch corrupted block 7\nReverting to factory defaults — manual override required',
        'RBS disc rotation stall at 22ms\nExpected 38ms full cycle — motor fault\nPort selection indeterminate — weapon unsafe',
        'Cortex signal flood detected\n12,400 events/ms — saturation threshold 800\nNeural interface shutting down pid:8812',
        'Neural key 0xA3F7 rejected by corp gate\nBIM chip reflashed — foreign employer signature\noperator locked out of own weapon',
        'HKB piston seized at stroke 3.1mm\nExpected 8mm — buffer failure\nRecoil unmitigated — operator injury risk',
        'Port beta empty — slug misfire\nExtractor found no casing in chamber\nRBS returned to alpha — 0 rounds remaining',
        'use-after-free in libneural.so.3 @ 0x7f3a\nHeap metadata overwritten addr=0x5582c4a0\nAddressSanitizer: SEGV on unknown address',
        'Stack buffer overflow in grip_classify()\nReturn address overwritten with 0x4141414141\nPossible exploitation attempt — abort',
        'Kernel OOPS: unable to handle paging request\nvirtual address 0xfffffffffffffff8\nRIP: bci_core+0x3a4/0x5c0',
        'KASAN: use-after-free in bim_calibrate()\nRead of size 8 at addr ffff888103a4\nBuggy address in object freed at bim_init+0x12',
        'Lockdep violation: possible deadlock\nhold: &bci_lock → &rbs_lock\nwait: &rbs_lock → &bci_lock  (cycle)',
        'Machine check: corrected error\nBank 4 MSR 0x411 = 0xbe00000000800400\nMemory controller: DRAM ECC correctable',
        'irqbalance: cpu2 lockup — self-test fail\nNMI watchdog: BUG soft lockup CPU#2 stuck 22s\nKernel panic: not syncing',
        'TCP connection reset by enforcement node\nconn 10.44.7.3:8443 → 172.16.0.1:443\nRST injected mid-handshake — active intercept?',
        'TLS certificate verification failed\ncert: glmz.relay.dist7 CN mismatch\nchain: expired intermediate 2024-11-30',
        'GLMZ gate 0x1b: blacklist match uid=3301\nDistrict 4 access revoked enforcement order\nAll outbound routes blocked — isolated',
        'District 12 relay offline — no heartbeat 120s\nFallback route dist7→dist9 congested 88%\nPacket loss 34% — connection degraded',
        'Corporate audit trail integrity check failed\nBlock 8192 hash mismatch expected=0x3af7\nTampering suspected — incident flagged',
        'Corp enclave attestation signature invalid\nExpected PCR[7]=0x2a3f got 0x0000\nPossible hypervisor injection — lockdown',
        'DB node corruption detected page 0x3301\nFSM state INVALID for block group 7\nFilesystem unmounted — journal replay required',
        'WAL log truncated unexpectedly at lsn 0x3af7\nExpected 4096 frames found 2048\nDatabase unrecoverable without backup',
        'Heap corruption detected in db/edge_insert\nChunk size 0x80 but next_size=0x21 (mismatch)\nMalloc abort — heap metadata invalid',
        'integer overflow in pkt_len calculation\nlen=0xffff0001 overflow wraps to 1\nBuffer underrun — memory corruption imminent',
        'format string vulnerability triggered\nprintf(user_buf) — controlled format string\nprocess 9144 terminated — possible exploit',
        'Signal 11 (SIGSEGV) at address 0x10\nLikely null pointer dereference + offset\npid 8812 bci_daemon — core dumped',
        'Signal 4 (SIGILL) — illegal instruction\nInvalid opcode at RIP=0x5582c4f0\nPossible code corruption or JIT error',
        'Signal 8 (SIGFPE) — arithmetic exception\nDivide by zero at net_rate_calc+0x4a\npid:8812 — process terminated',
        'OOM killer invoked\nKilling process 8812 (bci_daemon) score=892\nMem: 2097152kB total, 12kB free',
        'Disk full — /var/log 100% capacity\nWrite failed: No space left on device\nbci calibration log truncated — data loss',
        'fd limit exceeded: ulimit -n=1024\nprocess 8812 cannot open new file descriptors\nNeural calibration file handle leak suspected',
        'inode exhaustion on /var/bci\n0 inodes remaining on filesystem\nCannot create new calibration epoch files',
        'Syscall audit: unexpected execve() by pid 9144\nParent: bci_daemon — no exec expected\nPossible code injection — process sandboxed',
        'Seccomp violation: sys_ptrace by pid:9144\nKilled by SIGSYS — sandbox policy\nIntrusion attempt logged uid=1000',
        'SELinux denial: bci_daemon → corp_vault_t\npermission read on file /var/vault/key.pem\nAVC denied — policy enforcement active',
        'capabilities violation: CAP_NET_RAW denied\npid:9144 attempted raw socket creation\nProcess killed SIGKILL — policy enforced',
        'DMA overrun detected pci 0:1b.0\nDMA write outside mapped region 0x3af7000\nIOMMU fault — device reset required',
        'USB device 3-1.2 disconnected mid-transfer\nExpected 4096B transferred 1024B\nbci interface offline — reconnect required',
        'PCIe link down: device 0:1e.0\nlink_speed 8GT/s → link_down\nbci adapter offline — kernel driver error',
        'nvme0 I/O error: LBA 0x3af7 read fail\nError code: 0x4 (aborted command)\nFilesystem marked read-only — I/O error',
        'RAID array degraded: device /dev/sdb failed\n1 of 2 mirrors offline — no redundancy\nImminent data loss — replace drive',
        'Snapshot 0x3301 integrity check failed\nChecksum mismatch block 4096 of 8192\nRestore aborted — source corrupted',
        'Replication lag exceeds threshold: 44s\nPrimary LSN 0x3af7 replica 0x3a00\nAutomatic failover blocked — manual intervention',
        'Index 0x3301 B-tree root corrupted\nPage header magic 0xDEAD expected 0x3141\nDatabase offline — rebuild from backup',
        'Journal replay failed at lsn 0x1b\nTransaction incomplete — data loss\nManual recovery required: fsck.ext4',
        'SMART threshold exceeded: reallocated sectors=512\nPredicted failure imminent drive /dev/sda\nBackup immediately — hardware failure',
        'Memory ECC uncorrectable error\nDIMM slot A1 addr=0x7ffe3200\nSystem halted — hardware fault',
        'CPU thermal throttle: package temp 94°C\nPerformance reduced — cooling system fault\nProcess bci_daemon missed 22 deadlines',
        'Watchdog timeout: bci_daemon 30s no heartbeat\nAutomatic restart failed — respawn limit 5\nManual intervention required',
        'Zombie process storm: 4092 zombies\nParent pid:8812 not reaping children\nPID table exhaustion imminent',
        'Thread 8812 blocked 5min on mutex\nPossible deadlock: holder=thread 9144\nBacktrace: bim_calibrate → rbs_select → bci_sync',
        'CORP zero-day: privilege escalation attempt\nCVE-2025-8812 libneural.so.3 heap overflow\nPatch not applied — system vulnerable',
        'Rootkit signature detected in /proc\nhidden PID 31337 not in task_struct list\nKernel integrity compromised — incident',
        'Lateral movement detected\nuid=1000 → uid=0 via CVE-2025-8812\nPivot to corp_vault — exfil in progress',
        'Intrusion confirmed: corp_enclave breached\nForeign key material extracted — 4096B\nZero-day exploit — containment failed',
        'Network partition: district 7 → district 12\nAll relay routes timed out 30s\nGLMZ topology fragmented — traffic blocked',
        'ARP spoofing detected on 10.44.7.0/24\nGateway MAC changed 3x in 60s\nMan-in-the-middle attack suspected',
        'DNS hijack: glmz.relay.7 → 10.0.0.1\nExpected 172.16.44.12 got 10.0.0.1\nPhishing redirect — connection refused',
        'SYN flood on port 8443: 48k pkt/s\nHalf-open connections=65535 — table full\nbci_daemon connection refused — service down',
        'BGP route withdrawal: 10.44.0.0/16\nGLMZ district 4 unreachable\nAll paths withdrawn — isolation detected',
        'Firewall rule collision: rule 44 vs rule 7\nOverlapping src/dst — policy undefined\nPacket dropped — connection failure',
        'SSL_CTX_new() returned NULL\nOpenSSL error: lib(20) reason(65)\nOut of memory — TLS unavailable',
        'x509 certificate chain incomplete\nMissing intermediate CA at depth 1\nPeer verification failed — connection closed',
        'OCSP responder timeout: 5000ms\nCannot verify revocation status\nCertificate rejected — paranoid policy',
        'Diffie-Hellman key too small: 512 bits\nMinimum 2048 required by policy\nHandshake aborted — weak parameters',
        'HMAC verification failed: message tampered\nExpected 0x3af7c2d1 got 0xa1b2c3d4\nReplay attack? — connection terminated',
        'AES-GCM tag mismatch — decryption failed\nCiphertext: 64 bytes at 0x5582c4a0\nData integrity compromised — discard',
        'Random number generator failure\n/dev/random blocked — entropy pool empty\nCryptographic operations suspended',
        'Kernel canary check failed: pid:9144\nExpected 0x3af7deadbeef1337\nStack corruption — process killed',
        'ASLR bypass detected: same load addr 5x\nBase 0x5582c4a0 repeated across forks\nEntropy source failure — security degraded',
        'Control flow integrity violation\nIndirect call to 0x4141414141\nShadow stack mismatch — process terminated',
        'Spectre v2: indirect branch poisoning\nBranch predictor contamination detected\nProc 8812 isolation required — performance hit',
        'Meltdown mitigation disabled: KPTI=off\nKernel mapping exposed in user space\nSystem vulnerable — reboot required',
        'BCI foreign signature: employer key mismatch\nWeapon BIM expects corp_key=0x3301\nLockout enforced — weapon inoperable',
        'BCI motor cortex desync: 12k events lost\nBuffer overflow in neural_rx pipeline\nCalibration state corrupted — reset required',
        'BCI denial-of-sensation attack detected\nInput signal 0xFFFF flooding grip classifier\nOperator sensory loop disrupted — threat',
        'BCI lateral injection: foreign grip pattern\nClassifier trained on 0x3301 profile\nIntent classification unreliable — unsafe',
        'BCI key derivation failed: PBKDF2 abort\nSalt corrupted at byte 32 of 64\nNeural key unrecoverable — factory reset',
        'Corp policy violation: operator ID 3301\nUnauthorized district 4 access attempt\nAll credentials revoked — enforcement alerted',
        'Corp audit log gap: 300 entries missing\nTimestamp 2025-04-10T03:22 to T03:27\nLog tampering suspected — incident open',
        'Corp data exfiltration alert\n4.2MB outbound to 203.0.113.9 port 443\nDestination not in allowlist — blocked',
        'GLMZ enforcement beacon: lockdown active\nAll freelancer IDs suspended district 7\nbci_daemon forced offline by remote cmd',
        'GLMZ dark node exposure: IP 10.44.7.99\nNode appeared in GLMZ topology map\nOperator 3301 location triangulated',
        'GLMZ gate seizure: corp territorial acquisition\nDistrict 4 fabrication license revoked\nAll Crestfall BIM keys invalidated — mass lockout',
    ];

    // ── Floating code fragments ─────────────────────────────────────────────
    var FRAGS = [
        'for (int i = 0; i < n; i++) {\n    if (buf[i] == null) continue;\n    for (int j = 0; j < buf[i].Length; j++) {\n        if (!validator.Check(buf[i][j])) {\n            log.Warn($"invalid at [{i},{j}]");\n            fault.Record(i, j);\n            continue;\n        }\n        dispatch.Enqueue(buf[i][j]);\n    }\n    if (fault.Count > MAX_FAULTS) {\n        throw new IntegrityException(i);\n    }\n}',
        'while (!queue.IsEmpty()) {\n    var task = queue.Dequeue();\n    if (task.Priority == Priority.High) {\n        for (int r = 0; r < MAX_RETRY; r++) {\n            try {\n                await task.RunAsync(cts.Token);\n                break;\n            } catch (TimeoutException) {\n                if (r == MAX_RETRY - 1) throw;\n                await Task.Delay(backoff[r]);\n            }\n        }\n    } else {\n        pool.Submit(task);\n    }\n}',
        'switch (bci.IntentState) {\n    case IntentState.Precision:\n        if (rbs.CurrentPort != Port.Beta) {\n            await rbs.RotateAsync(Port.Beta);\n            led.Set(LedColor.Blue);\n            pin.Extend();\n        }\n        break;\n    case IntentState.Sweep:\n        if (rbs.CurrentPort != Port.Alpha) {\n            await rbs.RotateAsync(Port.Alpha);\n            led.Set(LedColor.Red);\n            pin.Retract();\n        }\n        break;\n    case IntentState.Transitioning:\n        led.Set(LedColor.Yellow);\n        await Task.Delay(RBS_ROTATION_MS);\n        break;\n    default:\n        if (mode != OverrideMode.Manual)\n            weapon.SafetyEngage();\n        break;\n}',
        'Task.Run(async () => {\n    while (!cts.IsCancellationRequested) {\n        for (int ring = 0; ring < CONTACT_RINGS; ring++) {\n            raw[ring] = adc.Sample(ring);\n            delta[ring] = raw[ring] - baseline[ring];\n            if (Math.Abs(delta[ring]) > NOISE_FLOOR) {\n                features[ring] = delta[ring] / baseline[ring];\n            } else {\n                features[ring] = 0f;\n            }\n        }\n        var intent = bim.Classify(features);\n        if (intent != lastIntent) {\n            rbs.RequestMode(intent);\n            lastIntent = intent;\n        }\n        await Task.Delay(POLL_INTERVAL_MS);\n    }\n});',
        'for (int epoch = 0; epoch < MAX_EPOCHS; epoch++) {\n    float acc = 0f;\n    for (int s = 0; s < samples.Count; s++) {\n        var pred = bim.Infer(samples[s].Features);\n        if (pred == samples[s].Label) acc++;\n    }\n    acc /= samples.Count;\n    if (acc >= TARGET_ACCURACY) {\n        bim.Commit();\n        log.Info($"cal done epoch={epoch} acc={acc:P1}");\n        break;\n    }\n    if (epoch == MAX_EPOCHS - 1) {\n        log.Warn("cal incomplete — defaulting");\n        bim.Revert();\n    }\n    bim.Adjust(lr * (1f - acc));\n}',
        'do {\n    var sig = cortex.Poll(POLL_TIMEOUT);\n    if (sig == null) { missedPolls++; continue; }\n    bim.Feed(sig);\n    if (bim.IsLocked) {\n        for (int i = 0; i < VERIFY_PASSES; i++) {\n            if (!bim.Verify(sig)) {\n                bim.Unlock();\n                missedPolls++;\n                break;\n            }\n        }\n    }\n    if (missedPolls > MAX_MISSED) {\n        log.Fatal("cortex signal lost");\n        throw new NeuralDesyncException();\n    }\n} while (!bim.IsLocked || missedPolls > 0);',
        'foreach (var node in graph.Nodes\n    .Where(n => n.District == targetDistrict)\n    .OrderBy(n => n.Latency)) {\n    if (node.IsBlacklisted) continue;\n    try {\n        var resp = await relay.PingAsync(node, PING_TIMEOUT);\n        if (resp.RTT < bestRTT) {\n            bestRTT  = resp.RTT;\n            bestNode = node;\n        }\n    } catch (TimeoutException) {\n        node.MissCount++;\n        if (node.MissCount > MAX_MISS) {\n            node.IsBlacklisted = true;\n            log.Warn($"node {node.Id} blacklisted");\n        }\n    }\n}',
        'for (int x = 0; x < W; x++) {\n    for (int y = 0; y < H; y++) {\n        float dx = Math.Min(x / (W * EDGE_FRAC), 1f);\n        float dy = Math.Min(y / (H * EDGE_FRAC), 1f);\n        float edge = Math.Min(dx, dy);\n        float n = fbm(\n            x / (float)W * NOISE_FREQ,\n            y / (float)H * NOISE_FREQ,\n            NOISE_OCTAVES);\n        float lo = n * TEAR_SCALE;\n        alpha[x, y] = Smoothstep(lo, lo + FEATHER, edge);\n        pixels[(y * W + x) * 4 + 3] =\n            (byte)(pixels[(y * W + x) * 4 + 3] * alpha[x, y]);\n    }\n}',
        'while (rbs.IsMoving) {\n    if (++elapsed > RBS_TIMEOUT_MS) {\n        rbs.EmergencyStop();\n        led.Set(LedColor.Dark);\n        switch (failMode) {\n            case FailMode.LockAlpha:\n                port = Port.Alpha;\n                break;\n            case FailMode.LockBeta:\n                port = Port.Beta;\n                break;\n            default:\n                weapon.SafetyEngage();\n                throw new RbsJamException(elapsed);\n        }\n        break;\n    }\n    await Task.Delay(1);\n}',
        'if (!enclave.Attest(expectedPcr)) {\n    log.Fatal("enclave attestation failed");\n    foreach (var key in vault.Keys.ToList()) {\n        try {\n            key.Revoke();\n            audit.Record(AuditEvent.KeyRevoked, key.Id);\n        } catch (Exception ex) {\n            log.Error($"revoke failed {key.Id}: {ex.Message}");\n        }\n    }\n    if (!lockdown.IsActive) {\n        lockdown.Engage(LockdownLevel.Hard);\n        await Task.Delay(LOCKDOWN_DELAY_MS);\n        net.Isolate();\n    }\n    Environment.Exit(ENCLAVE_FAIL_CODE);\n}',
        'Task.Run(async () => {\n    int failures = 0;\n    while (!cts.IsCancellationRequested) {\n        try {\n            await heartbeat.SendAsync(cts.Token);\n            failures = 0;\n        } catch (OperationCanceledException) {\n            break;\n        } catch (Exception ex) {\n            failures++;\n            log.Warn($"heartbeat fail #{failures}: {ex.Message}");\n            if (failures >= MAX_HB_FAILURES) {\n                log.Fatal("heartbeat dead — isolation");\n                net.Isolate();\n                return;\n            }\n        }\n        await Task.Delay(\n            failures == 0 ? INTERVAL_MS : INTERVAL_MS * (1 << failures),\n            cts.Token);\n    }\n});',
        'for (int page = 0; page < totalPages; page++) {\n    var buf = fs.ReadPage(page);\n    var actual   = Crc32.Compute(buf);\n    var expected = checksums[page];\n    if (actual != expected) {\n        corruptPages.Add(page);\n        log.Error($"page {page}: crc {actual:X8} != {expected:X8}");\n        if (corruptPages.Count > MAX_CORRUPT) {\n            throw new FilesystemCorruptException(\n                $"{corruptPages.Count} corrupt pages");\n        }\n        var recovered = journal.Recover(page);\n        if (recovered != null) {\n            fs.WritePage(page, recovered);\n            log.Info($"page {page} recovered from journal");\n        }\n    }\n}',
        'switch (pkType) {\n    case KeyType.Rsa4096:\n        if (!rsa.Verify(data, sig, cert.PublicKey)) {\n            throw new SignatureException("RSA-4096 invalid");\n        }\n        break;\n    case KeyType.EcdsaP256:\n        if (!ec.Verify(data, sig, cert.PublicKey)) {\n            throw new SignatureException("ECDSA-P256 invalid");\n        }\n        break;\n    case KeyType.Ed25519:\n        if (!ed.Verify(data, sig, cert.PublicKey)) {\n            throw new SignatureException("Ed25519 invalid");\n        }\n        break;\n    default:\n        audit.Alert(AuditEvent.UnknownKeyType, pkType);\n        throw new UnknownKeyTypeException(pkType);\n}',
        'while (wal.HasUnflushedFrames) {\n    var batch = wal.ReadBatch(CHECKPOINT_BATCH);\n    for (int i = 0; i < batch.Length; i++) {\n        db.ApplyFrame(batch[i]);\n        if (!db.VerifyFrame(batch[i])) {\n            log.Fatal($"WAL frame {batch[i].Lsn} corrupt");\n            db.RollbackTo(lastGoodLsn);\n            throw new WalCorruptException(batch[i].Lsn);\n        }\n        lastGoodLsn = batch[i].Lsn;\n    }\n    if (wal.FrameCount % CHECKPOINT_INTERVAL == 0) {\n        await wal.CheckpointAsync();\n        log.Debug($"checkpoint lsn={lastGoodLsn:X}");\n    }\n}',
        'foreach (var hop in route.Hops) {\n    if (enforcement.IsBlacklisted(hop.District)) {\n        log.Warn($"hop district {hop.District} blacklisted");\n        var alt = router.FindAlternate(hop, route.Destination);\n        if (alt == null) {\n            throw new NoRouteException(route.Destination);\n        }\n        route.Replace(hop, alt);\n        continue;\n    }\n    for (int attempt = 0; attempt < FORWARD_RETRIES; attempt++) {\n        if (relay.Forward(packet, hop)) break;\n        if (attempt == FORWARD_RETRIES - 1) {\n            hop.FailCount++;\n            route.Remove(hop);\n        }\n        await Task.Delay(RETRY_DELAY_MS << attempt);\n    }\n}',
        'do {\n    var chunk = heap.TryAlloc(size, alignment);\n    if (chunk != null) {\n        if (zeroed) chunk.Zero();\n        return chunk;\n    }\n    switch (oomPolicy) {\n        case OomPolicy.Collect:\n            gc.Collect(GcGeneration.All);\n            break;\n        case OomPolicy.Compact:\n            heap.Compact();\n            break;\n        case OomPolicy.Fail:\n            throw new OutOfMemoryException(\n                $"alloc {size}B align={alignment} failed");\n    }\n    retries++;\n} while (retries < MAX_ALLOC_RETRIES);',
        'for (int shard = 0; shard < vault.ShardCount; shard++) {\n    var s = vault.GetShard(shard);\n    if (!s.Verify(masterKey)) {\n        log.Warn($"shard {shard} verify failed — skipping");\n        badShards++;\n        continue;\n    }\n    unlockedShards++;\n    xorBuf.XorWith(s.KeyMaterial);\n    if (unlockedShards >= vault.Threshold) {\n        derivedKey = kdf.Derive(xorBuf, salt, KDF_ITERATIONS);\n        log.Info($"vault unsealed: {unlockedShards}/{vault.ShardCount}");\n        break;\n    }\n}\nif (unlockedShards < vault.Threshold) {\n    throw new VaultSealException(\n        $"only {unlockedShards} of {vault.Threshold} shards");\n}',
        'while (jit.HasPendingWork) {\n    var fn = jit.DequeueHotFunction();\n    if (fn.CallCount < JIT_THRESHOLD) continue;\n    try {\n        var ir  = lifter.Lift(fn.Bytecode);\n        var opt = optimizer.Run(ir, OptLevel.O2);\n        var mc  = emitter.Emit(opt);\n        jit.Install(fn, mc);\n        stats.JitCompiles++;\n    } catch (CompileException ex) {\n        log.Warn($"jit fail {fn.Name}: {ex.Message}");\n        fn.JitBlacklisted = true;\n    }\n    if (jit.CodeCacheUsed > JIT_CACHE_LIMIT) {\n        jit.EvictCold(EVICT_FRACTION);\n        log.Debug($"jit evict: cache={jit.CodeCacheUsed}B");\n    }\n}',
        'Task.Run(async () => {\n    while (!cts.IsCancellationRequested) {\n        sig = Syscall.Poll(fds, POLL_TIMEOUT);\n        if (sig < 0) {\n            if (sig == EINTR) continue;\n            throw new SyscallException("poll", sig);\n        }\n        foreach (var fd in fds.Where(f => f.IsReady)) {\n            switch (fd.Events) {\n                case PollEvent.Read:\n                    var data = fd.Read();\n                    if (data.Length == 0) { fd.Close(); break; }\n                    handler.OnData(fd, data);\n                    break;\n                case PollEvent.HangUp:\n                    handler.OnClose(fd);\n                    fd.Close();\n                    break;\n                case PollEvent.Error:\n                    handler.OnError(fd);\n                    break;\n            }\n        }\n    }\n});',
        'for (int i = 0; i < contactRings; i++) {\n    raw    = adc.Read(i);\n    delta  = raw - baseline[i];\n    if (Math.Abs(delta) <= noiseFl) {\n        features[i] = 0f;\n        continue;\n    }\n    features[i] = delta / (float)baseline[i];\n    if (features[i] > 0 && !seenPositive) {\n        seenPositive   = true;\n        precisionScore += features[i];\n    } else if (features[i] < 0) {\n        sweepScore -= features[i];\n    }\n}\nvar result =\n    precisionScore > sweepScore + HYSTERESIS\n        ? GripIntent.Precision\n        : sweepScore > precisionScore + HYSTERESIS\n            ? GripIntent.Sweep\n            : lastIntent;',
        'while (relay.BackoffMs < MAX_BACKOFF_MS) {\n    try {\n        var conn = await relay.ConnectAsync(\n            dist12, relay.BackoffMs, cts.Token);\n        if (conn.IsAuthenticated) {\n            log.Info($"relay up latency={conn.RTT}ms");\n            relay.BackoffMs = BASE_BACKOFF_MS;\n            return conn;\n        }\n        log.Warn("relay auth rejected — backoff");\n    } catch (OperationCanceledException) {\n        throw;\n    } catch (Exception ex) {\n        log.Warn($"relay fail: {ex.Message}");\n    }\n    relay.BackoffMs = Math.Min(\n        relay.BackoffMs * 2, MAX_BACKOFF_MS);\n    await Task.Delay(relay.BackoffMs, cts.Token);\n}\nthrow new RelayUnreachableException(dist12);',
        'if (oom.Score(proc) > OOM_THRESHOLD) {\n    var victims = procs\n        .Where(p => p.OomAdj >= 0)\n        .OrderByDescending(p => p.OomScore)\n        .Take(MAX_VICTIMS)\n        .ToList();\n    foreach (var v in victims) {\n        log.Warn($"oom kill pid:{v.Pid} ({v.Name}) rss={v.Rss}K");\n        v.Kill(Signal.SIGKILL);\n        freed += v.Rss;\n        if (freed >= needed) break;\n    }\n    if (freed < needed) {\n        log.Fatal("oom: insufficient memory after kills");\n        kernel.Panic("Out of memory");\n    }\n}',
        'for (int b = 0; b < wal.BlockCount; b++) {\n    var blk = wal.ReadBlock(b);\n    db.ApplyBlock(blk);\n    if (b % WAL_CHECKPOINT_EVERY == 0) {\n        wal.Checkpoint();\n        db.Sync();\n        log.Debug($"wal checkpoint @{b}/{wal.BlockCount}");\n    }\n    if (!db.IsConsistent()) {\n        log.Error($"inconsistency after block {b}");\n        db.RollbackToSnapshot(lastSnapshot);\n        wal.Truncate(b);\n        break;\n    }\n}',
        // 100 more
        'Task.Run(async () => {\n    await foreach (var evt in stream.ReadAllAsync(cts.Token)) {\n        switch (evt.Type) {\n            case EventType.GripUpdate:\n                bim.Feed(evt.Payload);\n                break;\n            case EventType.Disconnect:\n                bci.Reconnect();\n                break;\n            case EventType.Calibrate:\n                await bim.RecalibrateAsync();\n                break;\n            default:\n                log.Warn($"unhandled event {evt.Type}");\n                break;\n        }\n    }\n});',
        'for (int attempt = 0; attempt < MAX_RETRIES; attempt++) {\n    try {\n        var result = await gate.AuthorizeAsync(\n            token, scope, cts.Token);\n        if (result.IsGranted) {\n            session.Open(result.Claims);\n            audit.Log(AuditEvent.Login, token.Subject);\n            return result;\n        }\n        log.Warn($"auth denied: {result.Reason}");\n        await Task.Delay(DENY_COOLDOWN_MS);\n    } catch (TokenExpiredException) {\n        token = await token.RefreshAsync();\n    } catch (GateOfflineException) {\n        if (attempt == MAX_RETRIES - 1) throw;\n        await Task.Delay(OFFLINE_BACKOFF_MS << attempt);\n    }\n}\nthrow new AuthFailedException(MAX_RETRIES);',
        'while (net.HasPendingFrames) {\n    var frame = net.Dequeue();\n    if (!tls.Verify(frame)) {\n        dropped++;\n        if (dropped > MAX_DROPS) {\n            log.Error("too many invalid frames — closing");\n            net.Close();\n            break;\n        }\n        continue;\n    }\n    switch (frame.Type) {\n        case FrameType.Data:\n            rx.Append(frame.Payload);\n            break;\n        case FrameType.Ack:\n            tx.Acknowledge(frame.Seq);\n            break;\n        case FrameType.Reset:\n            net.Reset();\n            rx.Clear(); tx.Clear();\n            break;\n    }\n}',
        'foreach (var cert in chain) {\n    if (cert.NotAfter < DateTime.UtcNow) {\n        throw new CertExpiredException(\n            $"{cert.Subject} expired {cert.NotAfter}");\n    }\n    if (revoked.Contains(cert.Thumbprint)) {\n        throw new CertRevokedException(cert.Subject);\n    }\n    if (!prev.PublicKey.VerifySignature(\n            cert.TbsBytes, cert.Signature)) {\n        throw new ChainBrokenException(\n            $"sig invalid at depth {depth}");\n    }\n    depth++;\n    prev = cert;\n}',
        'for (int gen = GcGeneration.Young;\n     gen <= GcGeneration.Old; gen++) {\n    var roots = gc.ScanRoots(gen);\n    foreach (var root in roots) {\n        gc.Mark(root);\n    }\n    var swept = gc.Sweep(gen);\n    log.Debug(\n        $"gc gen{gen}: marked={gc.Marked} swept={swept}B");\n    if (gen == GcGeneration.Old &&\n        heap.Fragmentation > COMPACT_THRESHOLD) {\n        heap.Compact();\n        log.Info("heap compacted after full gc");\n    }\n}',
        'while (!shutdown.IsRequested) {\n    var batch = await ingest.ReadBatchAsync(\n        BATCH_SIZE, cts.Token);\n    if (batch.Count == 0) {\n        await Task.Delay(IDLE_DELAY_MS);\n        continue;\n    }\n    for (int i = 0; i < batch.Count; i++) {\n        try {\n            await processor.HandleAsync(batch[i]);\n            metrics.Processed++;\n        } catch (RetryableException ex) {\n            log.Warn($"retry item {i}: {ex.Message}");\n            await ingest.RequeueAsync(batch[i]);\n        } catch (PoisonException) {\n            log.Error($"poison item {i} — discarding");\n            metrics.Poisoned++;\n        }\n    }\n}',
        'if (bci.IsLinked && !manual.IsActive) {\n    var raw = new float[CONTACT_RINGS];\n    for (int r = 0; r < CONTACT_RINGS; r++) {\n        raw[r] = adc.Sample(r);\n    }\n    var intent = bim.Classify(raw);\n    if (intent != prev && confidence >= MIN_CONF) {\n        switch (intent) {\n            case GripIntent.Precision:\n                await rbs.SelectAsync(Port.Beta);\n                led.Set(Blue); pin.Extend();\n                break;\n            case GripIntent.Sweep:\n                await rbs.SelectAsync(Port.Alpha);\n                led.Set(Red); pin.Retract();\n                break;\n        }\n        prev = intent;\n    }\n}',
        'Task.Run(async () => {\n    using var timer = new PeriodicTimer(\n        TimeSpan.FromMilliseconds(SCHED_INTERVAL));\n    while (await timer.WaitForNextTickAsync(cts.Token)) {\n        for (int i = 0; i < workers.Count; i++) {\n            if (!workers[i].IsIdle) continue;\n            var job = scheduler.Dequeue(\n                workers[i].Affinity);\n            if (job == null) continue;\n            await workers[i].RunAsync(job);\n            metrics.Dispatched++;\n        }\n        if (scheduler.Depth > OVERFLOW_THRESHOLD) {\n            log.Warn($"queue depth={scheduler.Depth}");\n            await workers.ScaleAsync(+SCALE_STEP);\n        }\n    }\n});',
        'for (int row = 0; row < db.PageCount; row++) {\n    var page = db.ReadPage(row);\n    if (page.Magic != DB_MAGIC) {\n        log.Error($"bad magic page {row}: {page.Magic:X}");\n        corrupt.Add(row);\n        continue;\n    }\n    for (int slot = 0; slot < page.SlotCount; slot++) {\n        var rec = page.ReadSlot(slot);\n        if (!rec.IsAlive) continue;\n        if (idx.Contains(rec.Key)) {\n            log.Warn($"duplicate key {rec.Key} at p{row}s{slot}");\n            duplicates++;\n        } else {\n            idx.Add(rec.Key, new Pointer(row, slot));\n        }\n    }\n}',
        'while (jit.TraceBuf.Count > 0) {\n    var trace = jit.TraceBuf.Dequeue();\n    if (trace.HotCount < TRACE_HOT) continue;\n    try {\n        var ir   = tracer.BuildIR(trace);\n        var ssa  = ssa.Convert(ir);\n        var opt1 = dce.Run(ssa);\n        var opt2 = gvn.Run(opt1);\n        var opt3 = licm.Run(opt2);\n        var mc   = regAlloc.Allocate(opt3);\n        var ptr  = emitter.Emit(mc);\n        jit.PatchTrace(trace.EntryAddr, ptr);\n    } catch (TraceAbortException ex) {\n        log.Debug($"trace abort: {ex.Reason}");\n        jit.Blacklist(trace.EntryAddr);\n    }\n}',
        'foreach (var edge in graph.Edges\n    .Where(e => !e.IsValid(DateTime.UtcNow))\n    .ToList()) {\n    graph.RemoveEdge(edge);\n    var src  = graph.GetNode(edge.SourceId);\n    var dst  = graph.GetNode(edge.TargetId);\n    if (src != null) src.Degree--;\n    if (dst != null) dst.Degree--;\n    if (src?.Degree == 0 && src.IsPrunable) {\n        graph.RemoveNode(src.Id);\n        pruned.Nodes++;\n    }\n    pruned.Edges++;\n}\nlog.Info($"graph pruned: {pruned.Nodes}n {pruned.Edges}e");',
        'do {\n    token = await oauth.RequestAsync(\n        clientId, scope, cts.Token);\n    if (token.IsValid) break;\n    log.Warn($"token invalid: {token.Error}");\n    if (token.Error == OAuthError.Revoked) {\n        await oauth.RevokeAsync(token);\n        throw new AuthRevokedException();\n    }\n    retries++;\n    if (retries >= MAX_TOKEN_RETRIES) {\n        throw new TokenExhaustedException();\n    }\n    await Task.Delay(\n        TOKEN_BACKOFF_MS * (1 << retries));\n} while (!token.IsValid);',
        'for (int pass = 0; pass < ENCRYPT_PASSES; pass++) {\n    for (int blk = 0; blk < data.BlockCount; blk++) {\n        var plain  = data.ReadBlock(blk);\n        var iv     = rng.NextBytes(IV_LEN);\n        var cipher = aes.Encrypt(plain, iv, key);\n        var tag    = hmac.Sign(cipher, authKey);\n        out.WriteBlock(blk, iv, cipher, tag);\n        if (blk % PROGRESS_INTERVAL == 0) {\n            progress?.Report(\n                (pass * data.BlockCount + blk) /\n                (float)(ENCRYPT_PASSES * data.BlockCount));\n        }\n    }\n    key = kdf.Derive(key, salt, 1);\n}',
        'while (socket.State == WebSocketState.Open) {\n    var result = await socket.ReceiveAsync(\n        buffer, cts.Token);\n    if (result.MessageType == Close) {\n        await socket.CloseAsync(\n            WebSocketCloseStatus.NormalClosure,\n            "bye", cts.Token);\n        break;\n    }\n    accumulated.Write(\n        buffer.Array, 0, result.Count);\n    if (result.EndOfMessage) {\n        var msg = Encoding.UTF8.GetString(\n            accumulated.ToArray());\n        await dispatcher.HandleAsync(msg);\n        accumulated.SetLength(0);\n    }\n}',
        'Task.Run(async () => {\n    var sema = new SemaphoreSlim(\n        MAX_CONCURRENT_OPS);\n    var tasks = items.Select(async item => {\n        await sema.WaitAsync(cts.Token);\n        try {\n            await processor.ProcessAsync(\n                item, cts.Token);\n            Interlocked.Increment(ref done);\n        } catch (Exception ex) {\n            log.Error(\n                $"process {item.Id}: {ex.Message}");\n            Interlocked.Increment(ref failed);\n        } finally {\n            sema.Release();\n        }\n    });\n    await Task.WhenAll(tasks);\n    log.Info($"done={done} failed={failed}");\n});',
        'for (int i = 0; i < bitmap.Width; i++) {\n    for (int j = 0; j < bitmap.Height; j++) {\n        int idx = (j * bitmap.Stride + i * 4);\n        float lum = (\n            pixels[idx + 0] * 0.2126f +\n            pixels[idx + 1] * 0.7152f +\n            pixels[idx + 2] * 0.0722f) / 255f;\n        float alpha = tornEdge.Sample(i, j);\n        pixels[idx + 3] = (byte)(\n            pixels[idx + 3] * alpha *\n            (0.6f + lum * 0.4f));\n    }\n}',
        'switch (packet.Protocol) {\n    case Protocol.Tcp:\n        if (!tcp.IsValidChecksum(packet)) {\n            dropped++; break;\n        }\n        switch (tcp.Flags) {\n            case TcpFlags.Syn:\n                connTable.OpenHalf(packet);\n                break;\n            case TcpFlags.SynAck:\n                connTable.Complete(packet);\n                break;\n            case TcpFlags.Fin:\n            case TcpFlags.Rst:\n                connTable.Close(packet);\n                break;\n        }\n        break;\n    case Protocol.Udp:\n        udp.Route(packet);\n        break;\n    case Protocol.Icmp:\n        if (firewall.AllowIcmp) icmp.Handle(packet);\n        break;\n}',
        'while (compressor.HasInput) {\n    var block = compressor.ReadInput(BLOCK_SIZE);\n    var lz4   = Lz4.Compress(block);\n    if (lz4.Length >= block.Length) {\n        out.WriteUncompressed(block);\n        stats.Uncompressed += block.Length;\n    } else {\n        out.WriteCompressed(lz4);\n        stats.Compressed   += block.Length;\n        stats.Saved        += block.Length - lz4.Length;\n    }\n    if (out.Position % FLUSH_INTERVAL == 0) {\n        await out.FlushAsync();\n    }\n}\nlog.Info($"ratio={stats.Ratio:F2} saved={stats.Saved}B");',
        'foreach (var proc in procTable.Values\n    .Where(p => p.State == ProcState.Zombie)\n    .ToList()) {\n    if (!proc.Parent.IsAlive) {\n        proc.Reparent(init);\n    }\n    var exit = proc.Reap();\n    log.Debug(\n        $"reaped pid:{proc.Pid} exit:{exit.Code}");\n    if (exit.Signal != Signal.None) {\n        log.Warn(\n            $"pid:{proc.Pid} killed by {exit.Signal}");\n    }\n    procTable.Remove(proc.Pid);\n    resources.Release(proc);\n}',
        'for (int lvl = 0; lvl < BTREE_LEVELS; lvl++) {\n    var node = btree.ReadNode(cursor.NodeId);\n    if (node.IsLeaf) {\n        var slot = node.FindSlot(key);\n        if (slot < 0) return null;\n        return node.ReadValue(slot);\n    }\n    int child = -1;\n    for (int k = 0; k < node.KeyCount; k++) {\n        if (key.CompareTo(node.Keys[k]) < 0) {\n            child = node.Children[k];\n            break;\n        }\n    }\n    if (child < 0)\n        child = node.Children[node.KeyCount];\n    cursor.NodeId = child;\n}',
        'Task.Run(async () => {\n    while (!cts.IsCancellationRequested) {\n        var snap = metrics.Snapshot();\n        foreach (var (key, val) in snap) {\n            await telemetry.EmitAsync(\n                key, val, DateTime.UtcNow);\n        }\n        if (snap.TryGetValue("heap_rss", out var rss)\n            && rss > RSS_ALERT_THRESHOLD) {\n            await alerting.FireAsync(\n                Alert.HighMemory, rss);\n        }\n        if (snap.TryGetValue("error_rate", out var er)\n            && er > ERROR_RATE_THRESHOLD) {\n            await alerting.FireAsync(\n                Alert.HighErrorRate, er);\n        }\n        await Task.Delay(METRICS_INTERVAL_MS);\n    }\n});',
        'for (int i = 0; i < nodes.Count; i++) {\n    for (int j = i + 1; j < nodes.Count; j++) {\n        if (!graph.HasEdge(nodes[i], nodes[j])) {\n            continue;\n        }\n        float w = similarity.Compute(\n            nodes[i].Embedding,\n            nodes[j].Embedding);\n        if (w < PRUNE_THRESHOLD) {\n            graph.RemoveEdge(nodes[i], nodes[j]);\n            pruned++;\n        } else if (w > MERGE_THRESHOLD) {\n            candidates.Enqueue(\n                (nodes[i], nodes[j], w));\n        }\n    }\n}',
        'while (input.HasMore) {\n    var token = lexer.Next();\n    switch (token.Kind) {\n        case TokenKind.If:\n            ast.Push(new IfNode());\n            break;\n        case TokenKind.While:\n            ast.Push(new WhileNode());\n            break;\n        case TokenKind.For:\n            ast.Push(new ForNode());\n            break;\n        case TokenKind.LBrace:\n            ast.OpenBlock();\n            break;\n        case TokenKind.RBrace:\n            var block = ast.CloseBlock();\n            ast.Peek().AddChild(block);\n            break;\n        case TokenKind.Eof:\n            return ast.Root;\n    }\n}',
        'foreach (var region in memMap.Regions) {\n    if (!region.Flags.HasFlag(MemFlags.Write)) {\n        continue;\n    }\n    for (ulong addr = region.Base;\n         addr < region.Base + region.Size;\n         addr += PAGE_SIZE) {\n        var page = mem.ReadPage(addr);\n        if (page.IsDirty) {\n            var hash = sha256.Hash(page.Data);\n            if (hash != page.ExpectedHash) {\n                tampered.Add(addr);\n                log.Warn(\n                    $"page tamper @{addr:X16}");\n            }\n            page.ClearDirty();\n        }\n    }\n}',
        'for (int r = 0; r < MAX_RETRIES; r++) {\n    try {\n        using var tx = db.BeginTransaction(\n            IsolationLevel.Serializable);\n        var existing = tx.Find<Node>(id);\n        if (existing != null) {\n            existing.Update(payload);\n        } else {\n            tx.Insert(new Node(id, payload));\n        }\n        await tx.CommitAsync();\n        return;\n    } catch (SerializationConflict) {\n        if (r == MAX_RETRIES - 1) throw;\n        await Task.Delay(\n            TX_BACKOFF_MS * (1 << r));\n    }\n}',
        'Task.Run(async () => {\n    var ring = new RingBuffer<Event>(RING_SIZE);\n    while (!cts.IsCancellationRequested) {\n        int count = producer.Drain(ring);\n        for (int i = 0; i < count; i++) {\n            var ev = ring[i];\n            switch (ev.Category) {\n                case Category.Audit:\n                    await auditLog.WriteAsync(ev);\n                    break;\n                case Category.Metric:\n                    metrics.Record(ev);\n                    break;\n                case Category.Alert:\n                    await alerting.NotifyAsync(ev);\n                    break;\n            }\n        }\n        if (count == 0)\n            await Task.Yield();\n    }\n});',
        'while (fsm.State != State.Terminal) {\n    var input = fsm.NextInput();\n    switch (fsm.State) {\n        case State.Idle:\n            if (input.Is(Input.Connect))\n                fsm.Transition(State.Handshake);\n            break;\n        case State.Handshake:\n            if (!tls.Complete(input))\n                fsm.Transition(State.Error);\n            else\n                fsm.Transition(State.Active);\n            break;\n        case State.Active:\n            session.Process(input);\n            if (input.Is(Input.Close))\n                fsm.Transition(State.Closing);\n            break;\n        case State.Error:\n            log.Error($"fsm error: {input}");\n            fsm.Transition(State.Terminal);\n            break;\n    }\n}',
        'for (int oct = 0; oct < OCTAVES; oct++) {\n    float freq  = BASE_FREQ * MathF.Pow(2f, oct);\n    float amp   = BASE_AMP  * MathF.Pow(PERSIST, oct);\n    for (int y = 0; y < H; y++) {\n        for (int x = 0; x < W; x++) {\n            float nx = x * freq / W;\n            float ny = y * freq / H;\n            noise[y * W + x] +=\n                vnoise(nx, ny, seed + oct) * amp;\n        }\n    }\n}\nfor (int i = 0; i < W * H; i++) {\n    noise[i] = (noise[i] + 1f) * 0.5f;\n    noise[i] = MathF.Pow(noise[i], CONTRAST);\n}',
        'foreach (var tx in pending.OrderBy(t => t.Fee)) {\n    if (block.ByteSize + tx.Size > MAX_BLOCK_SIZE)\n        break;\n    if (utxo.IsDoubleSpend(tx)) {\n        log.Warn($"double spend tx {tx.Hash}");\n        continue;\n    }\n    foreach (var input in tx.Inputs) {\n        if (!utxo.Contains(input.OutPoint)) {\n            log.Warn($"missing utxo {input.OutPoint}");\n            goto nextTx;\n        }\n    }\n    utxo.Apply(tx);\n    block.AddTransaction(tx);\n    nextTx:;\n}',
        'Task.Run(async () => {\n    var limiter = new RateLimiter(\n        RATE_LIMIT, TimeSpan.FromSeconds(1));\n    await foreach (var req in requests\n        .ReadAllAsync(cts.Token)) {\n        if (!await limiter.TryAcquireAsync()) {\n            await req.RespondAsync(\n                StatusCode.TooManyRequests);\n            metrics.Throttled++;\n            continue;\n        }\n        var resp = await handler.HandleAsync(req);\n        await req.RespondAsync(resp);\n        metrics.Handled++;\n    }\n});',
        'for (int i = threadpool.MinThreads;\n     i <= threadpool.MaxThreads; i++) {\n    var w = new Worker(i, queue);\n    workers.Add(w);\n    w.Start();\n    if (queue.Depth < SCALE_DOWN_THRESHOLD\n        && i > threadpool.MinThreads) {\n        log.Debug($"pool: capped at {i} workers");\n        break;\n    }\n}\nlog.Info(\n    $"threadpool: {workers.Count} workers active");',
        'while (cts.IsCancellationRequested == false) {\n    var req = await listener.AcceptAsync(cts.Token);\n    _ = Task.Run(async () => {\n        try {\n            if (!rateLimit.Allow(req.RemoteEp)) {\n                await req.RejectAsync(429);\n                return;\n            }\n            var resp = await router.RouteAsync(req);\n            await req.SendAsync(resp);\n            access.Log(req, resp);\n        } catch (Exception ex) {\n            log.Error(\n                $"req {req.Id}: {ex.Message}");\n            await req.RejectAsync(500);\n        }\n    }, cts.Token);\n}',
        'for (int page = startPage;\n     page <= endPage; page++) {\n    if (snapshot.Bitmap.IsSet(page)) {\n        var data = cow.ReadVersion(page, version);\n        out.WritePage(page, data);\n    } else {\n        var data = db.ReadPage(page);\n        out.WritePage(page, data);\n        snapshot.Bitmap.Set(page);\n    }\n    if ((page - startPage) % FLUSH_PAGES == 0) {\n        await out.FlushAsync();\n        progress?.Report(\n            (page - startPage) /\n            (float)(endPage - startPage));\n    }\n}',
        'switch (sig.Value) {\n    case Signal.SIGINT:\n    case Signal.SIGTERM:\n        log.Info($"caught {sig} — graceful shutdown");\n        await server.StopAsync(GRACEFUL_TIMEOUT);\n        break;\n    case Signal.SIGHUP:\n        log.Info("SIGHUP — reloading config");\n        await config.ReloadAsync();\n        await server.ReconfigureAsync(config);\n        break;\n    case Signal.SIGUSR1:\n        log.Info("SIGUSR1 — rotating logs");\n        await logger.RotateAsync();\n        break;\n    case Signal.SIGSEGV:\n        log.Fatal("SIGSEGV — crash dump");\n        await core.DumpAsync();\n        Environment.FailFast("SIGSEGV");\n        break;\n}',
        'Task.Run(async () => {\n    var pending = new List<LogEntry>();\n    while (!cts.IsCancellationRequested) {\n        var entry = await logQueue\n            .ReadAsync(cts.Token);\n        pending.Add(entry);\n        if (pending.Count >= FLUSH_BATCH ||\n            (DateTime.UtcNow - pending[0].Time)\n                > FLUSH_INTERVAL) {\n            try {\n                await sink.WriteBatchAsync(pending);\n                pending.Clear();\n            } catch (Exception ex) {\n                log.Error(\n                    $"log flush failed: {ex.Message}");\n                await Task.Delay(RETRY_DELAY);\n            }\n        }\n    }\n    if (pending.Count > 0)\n        await sink.WriteBatchAsync(pending);\n});',
        'for (int seg = 0; seg < segs.Count; seg++) {\n    var s = segs[seg];\n    if (s.Compression == Codec.None) {\n        out.Write(s.Data);\n    } else if (s.Compression == Codec.Lz4) {\n        out.Write(Lz4.Decompress(\n            s.Data, s.UncompressedLen));\n    } else if (s.Compression == Codec.Zstd) {\n        out.Write(Zstd.Decompress(s.Data));\n    } else {\n        throw new UnknownCodecException(\n            s.Compression);\n    }\n    if (s.Checksum != Crc32.Compute(s.Data)) {\n        throw new SegmentCorruptException(seg);\n    }\n}',
        'while (!input.IsEof) {\n    int ch = input.Peek();\n    if (ch == \'/\') {\n        input.Advance();\n        if (input.Peek() == \'/\') {\n            while (!input.IsEol) input.Advance();\n        } else if (input.Peek() == \'*\') {\n            while (!input.IsEof) {\n                input.Advance();\n                if (prev == \'*\' && input.Peek() == \'/\') {\n                    input.Advance(); break;\n                }\n                prev = (char)input.Peek();\n            }\n        } else {\n            yield return Token(TokenKind.Slash);\n        }\n    } else {\n        yield return Scan(input);\n    }\n}',
        'foreach (var shard in ring\n    .GetPreferredNodes(key, REPLICAS)) {\n    var ok = false;\n    for (int t = 0; t < WRITE_TRIES; t++) {\n        try {\n            await shard.PutAsync(\n                key, value, ttl, cts.Token);\n            ok = true; break;\n        } catch (NodeUnavailableException) {\n            var next = ring.NextNode(shard);\n            if (next == null) break;\n            shard = next;\n        }\n    }\n    if (ok) acks++;\n    if (acks >= WRITE_QUORUM) return;\n}\nif (acks < WRITE_QUORUM)\n    throw new QuorumException(acks, WRITE_QUORUM);',
        'Task.Run(async () => {\n    await using var conn = await pool.RentAsync();\n    while (!cts.IsCancellationRequested) {\n        var changes = await cdc.PollAsync(\n            lastLsn, cts.Token);\n        foreach (var ch in changes) {\n            switch (ch.Op) {\n                case CdcOp.Insert:\n                    await sink.OnInsert(ch.Row);\n                    break;\n                case CdcOp.Update:\n                    await sink.OnUpdate(\n                        ch.OldRow, ch.Row);\n                    break;\n                case CdcOp.Delete:\n                    await sink.OnDelete(ch.Row);\n                    break;\n            }\n            lastLsn = ch.Lsn;\n        }\n    }\n});',
        'for (int level = 0;\n     level < lsm.LevelCount; level++) {\n    var files = lsm.GetLevel(level);\n    if (files.Count < lsm.LevelMaxFiles(level))\n        continue;\n    var merged = Sstable.Merge(files);\n    foreach (var f in files) {\n        lsm.Remove(f);\n        f.Delete();\n    }\n    if (level + 1 < lsm.LevelCount) {\n        lsm.AddToLevel(level + 1, merged);\n    } else {\n        lsm.AddNewLevel(merged);\n    }\n    log.Info(\n        $"compacted L{level}: {files.Count}→1");\n}',
        'while (reader.TryRead(out var span)) {\n    for (int i = 0; i < span.Length; i++) {\n        ref var sample = ref span[i];\n        if (sample.Value > CLIP_HIGH) {\n            sample.Value = CLIP_HIGH;\n            clipped++;\n        } else if (sample.Value < CLIP_LOW) {\n            sample.Value = CLIP_LOW;\n            clipped++;\n        }\n        sum += sample.Value;\n        sumSq += sample.Value * sample.Value;\n        count++;\n    }\n    if (count % STATS_INTERVAL == 0) {\n        float mean = sum / count;\n        float var_ = sumSq / count - mean * mean;\n        telemetry.Record("signal_var", var_);\n    }\n}',
        'foreach (var group in events\n    .GroupBy(e => e.Category)\n    .OrderByDescending(g => g.Count())) {\n    var agg = new Aggregate {\n        Category = group.Key,\n        Count    = group.Count(),\n        Total    = group.Sum(e => e.Value),\n        P50      = group.Percentile(e => e.Value, 50),\n        P99      = group.Percentile(e => e.Value, 99),\n    };\n    if (agg.P99 > SLA_THRESHOLD) {\n        await alerting.FireAsync(\n            Alert.SlaBreach, agg);\n    }\n    await metrics.EmitAsync(agg);\n}',
        'Task.Run(async () => {\n    var buf = new byte[RECV_BUF];\n    while (true) {\n        int n = await udp.ReceiveAsync(\n            buf, cts.Token);\n        if (n < HEADER_LEN) continue;\n        var hdr = Header.Parse(buf, n);\n        if (!hdr.IsValid) {\n            malformed++; continue;\n        }\n        if (seen.Contains(hdr.Nonce)) {\n            replays++; continue;\n        }\n        seen.Add(hdr.Nonce);\n        var plain = chacha.Decrypt(\n            buf, HEADER_LEN, n - HEADER_LEN,\n            hdr.Nonce, sessionKey);\n        if (plain == null) {\n            authFails++; continue;\n        }\n        await dispatcher.HandleAsync(plain);\n    }\n});',
        'for (int r = 0; r < rows; r++) {\n    for (int c = 0; c < cols; c++) {\n        float s = 0f;\n        for (int k = 0; k < K; k++) {\n            s += A[r * K + k] * B[k * cols + c];\n        }\n        C[r * cols + c] = s;\n    }\n    if (r % PROGRESS_ROWS == 0) {\n        float pct = r / (float)rows * 100f;\n        log.Debug(\n            $"matmul: {pct:F0}% ({r}/{rows})");\n    }\n}',
        'while (!drain.IsComplete) {\n    var slot = ring.TryAcquireSlot();\n    if (slot < 0) {\n        await Task.Yield();\n        continue;\n    }\n    try {\n        var item = source.Take(cts.Token);\n        ring.Write(slot, Serialize(item));\n        ring.Publish(slot);\n        produced++;\n    } catch (InvalidOperationException) {\n        ring.Release(slot);\n        drain.Signal();\n    } catch (OperationCanceledException) {\n        ring.Release(slot);\n        break;\n    }\n}',
        'foreach (var (node, depth) in bfs.Traverse(root)) {\n    if (depth > MAX_DEPTH) {\n        bfs.Prune(node);\n        continue;\n    }\n    switch (node.Type) {\n        case NodeType.Entity:\n            index.Add(node.Id, node);\n            break;\n        case NodeType.Relation:\n            graph.Link(\n                node.SourceId,\n                node.TargetId,\n                node.Weight);\n            break;\n        case NodeType.Aggregate:\n            stats.Accumulate(node);\n            break;\n    }\n    if (node.HasChildren)\n        bfs.Enqueue(node.Children, depth + 1);\n}',
        'Task.Run(async () => {\n    await using var cursor = db.OpenCursor(\n        table, scanOrder);\n    while (await cursor.MoveNextAsync()) {\n        var row = cursor.Current;\n        if (predicate(row)) {\n            buffer.Add(row);\n            if (buffer.Count >= BATCH_SIZE) {\n                await handler.HandleBatchAsync(\n                    buffer);\n                buffer.Clear();\n            }\n        }\n    }\n    if (buffer.Count > 0) {\n        await handler.HandleBatchAsync(buffer);\n    }\n    log.Info(\n        $"scan done: {cursor.Scanned} rows");\n});',
        'for (int i = 0; i < proof.Length; i++) {\n    if (i % 2 == 0) {\n        current = sha256.Hash(\n            Concat(current, proof[i]));\n    } else {\n        current = sha256.Hash(\n            Concat(proof[i], current));\n    }\n    if (current == null ||\n        current.Length != HASH_LEN) {\n        throw new MerkleInvalidException(\n            $"bad hash at depth {i}");\n    }\n}\nif (!current.SequenceEqual(root)) {\n    throw new MerkleProofException();\n}',
        'while (freeList.Count < MIN_FREE_PAGES) {\n    var victim = clock.NextVictim();\n    if (victim == null) break;\n    if (victim.IsPinned) {\n        clock.Skip(victim);\n        continue;\n    }\n    if (victim.IsDirty) {\n        await disk.WriteAsync(\n            victim.PageId, victim.Data);\n        dirty--;\n    }\n    bufferPool.Evict(victim);\n    freeList.Add(victim.Frame);\n    evictions++;\n}',
        'foreach (var pipeline in pipelines) {\n    var result = pipeline.Source;\n    foreach (var stage in pipeline.Stages) {\n        try {\n            result = await stage.ProcessAsync(\n                result, cts.Token);\n        } catch (StageException ex) {\n            if (stage.CanBypass) {\n                log.Warn(\n                    $"bypass {stage.Name}: {ex.Message}");\n                continue;\n            }\n            throw;\n        }\n    }\n    await pipeline.Sink.WriteAsync(result);\n}',
        'for (int gen = 0; gen < GENERATIONS; gen++) {\n    var selected = population\n        .OrderByDescending(c => c.Fitness)\n        .Take(ELITE_COUNT)\n        .ToList();\n    while (selected.Count < POP_SIZE) {\n        var p1 = tournament.Select(population);\n        var p2 = tournament.Select(population);\n        var child = crossover.Apply(p1, p2);\n        if (rng.NextDouble() < MUTATION_RATE) {\n            mutation.Apply(child);\n        }\n        selected.Add(child);\n    }\n    population = selected;\n    var best = population.Max(c => c.Fitness);\n    log.Debug($"gen {gen}: best={best:F4}");\n}',
        'Task.Run(async () => {\n    var debounce = new Debouncer(\n        TimeSpan.FromMilliseconds(DEBOUNCE_MS));\n    await foreach (var change in watcher\n        .WatchAsync(cts.Token)) {\n        debounce.Trigger(change.Path, async () => {\n            try {\n                await handler.OnChangeAsync(\n                    change);\n                log.Info(\n                    $"reloaded {change.Path}");\n            } catch (Exception ex) {\n                log.Error(\n                    $"reload fail: {ex.Message}");\n            }\n        });\n    }\n});',
        'while (backpressure.IsHighWatermark) {\n    var evicted = 0;\n    foreach (var entry in cache\n        .OrderBy(e => e.LastAccess)\n        .Take(EVICT_BATCH)) {\n        if (entry.IsPinned) continue;\n        cache.Remove(entry.Key);\n        evicted++;\n        if (!backpressure.IsHighWatermark)\n            break;\n    }\n    if (evicted == 0) {\n        log.Warn("cache pressure: nothing evictable");\n        await Task.Delay(BACKPRESSURE_WAIT_MS);\n    }\n}',
        'for (int seg = segStart; seg < segEnd; seg++) {\n    var hdr = elf.ReadSegmentHeader(seg);\n    if (hdr.Type != PT_LOAD) continue;\n    var data = elf.ReadSegment(seg);\n    var vaddr = hdr.VAddr + baseAddr;\n    mem.Map(vaddr, hdr.MemSize, hdr.Flags);\n    mem.Write(vaddr, data);\n    if (data.Length < hdr.MemSize) {\n        mem.Zero(\n            vaddr + data.Length,\n            hdr.MemSize - data.Length);\n    }\n    if (hdr.Flags.HasFlag(PF_X)) {\n        cache.FlushICache(vaddr, hdr.MemSize);\n    }\n}',
        'foreach (var bucket in histogram\n    .GetBuckets()\n    .OrderBy(b => b.LowerBound)) {\n    float density =\n        bucket.Count / (float)histogram.Total;\n    for (int col = 0;\n         col < (int)(density * MAX_COLS);\n         col++) {\n        row.Append(\'█\');\n    }\n    row.Append(\n        $" [{bucket.LowerBound:F2}–"\n        + $"{bucket.UpperBound:F2}]"\n        + $" n={bucket.Count}");\n    output.WriteLine(row.ToString());\n    row.Clear();\n}',
        'Task.Run(async () => {\n    using var scope = services.CreateScope();\n    var repo = scope.Get<INodeRepository>();\n    while (!cts.IsCancellationRequested) {\n        var stale = await repo.FindStaleAsync(\n            olderThan: DateTime.UtcNow - TTL,\n            limit: PURGE_BATCH);\n        if (stale.Count == 0) {\n            await Task.Delay(IDLE_PERIOD_MS);\n            continue;\n        }\n        foreach (var n in stale) {\n            await repo.ArchiveAsync(n);\n            purged++;\n        }\n        log.Info(\n            $"purged {stale.Count} nodes total={purged}");\n    }\n});',
        'for (int i = 0; i < seq.Count - 1; i++) {\n    int gap = seq[i + 1].Timestamp\n            - seq[i].Timestamp;\n    if (gap < MIN_GAP || gap > MAX_GAP) {\n        anomalies.Add(new Anomaly {\n            Index = i,\n            Expected = NOMINAL_GAP,\n            Actual   = gap,\n            Severity = gap > MAX_GAP\n                ? Severity.High\n                : Severity.Low\n        });\n        if (anomalies.Count > MAX_ANOMALIES) {\n            throw new AnomalyFloodException();\n        }\n    }\n}',
    ];

    var tickTimer = null;

    function rand(a, b) { return Math.floor(Math.random() * (b - a + 1)) + a; }
    function pick(arr)  { return arr[rand(0, arr.length - 1)]; }
    function getHost()  { return document.querySelector('.console-bg-host'); }

    // ── Terminal windows ────────────────────────────────────────────────────

    function spawnWindow(extraDelay, posX, posY) {
        setTimeout(function () {
            var host = getHost();
            if (!host) return;

            // ~20% chance the window lingers, waiting for input
            var waiting = Math.random() < 0.20;

            var win = document.createElement('div');
            win.className = 'cbg-win';
            win.style.left = (posX !== undefined ? posX : rand(1, 70)) + '%';
            win.style.top  = (posY !== undefined ? posY : rand(4, 76)) + '%';

            var titleEl = document.createElement('div');
            titleEl.className = 'cbg-title';
            titleEl.textContent = pick(TITLES);
            win.appendChild(titleEl);

            var body = document.createElement('div');
            body.className = 'cbg-body';
            win.appendChild(body);

            host.appendChild(win);

            var lineCount = rand(2, 6);
            var lines = [];
            for (var i = 0; i < lineCount; i++) { lines.push(pick(LINES)); }

            var success = !waiting && Math.random() > 0.32;
            var result  = pick(success ? OK_RESULTS : ERR_RESULTS);
            var resCls  = success ? 'cbg-ok' : 'cbg-err';

            var idx = 0;
            var lineTimer = setInterval(function () {
                if (idx < lines.length) {
                    var ln = document.createElement('div');
                    ln.className = 'cbg-line';
                    ln.textContent = lines[idx];
                    body.appendChild(ln);
                    idx++;
                } else {
                    clearInterval(lineTimer);
                    setTimeout(function () {
                        if (waiting) {
                            // Show a prompt line with blinking cursor — no result, lingers
                            var prompt = document.createElement('div');
                            prompt.className = 'cbg-line cbg-prompt';
                            prompt.innerHTML = '> <span class="cbg-cursor">&#9608;</span>';
                            body.appendChild(prompt);
                            // Long random TTL: 6–18 seconds
                            setTimeout(function () {
                                win.classList.add('cbg-win--out');
                                setTimeout(function () {
                                    if (win.parentNode) win.parentNode.removeChild(win);
                                }, 160);
                            }, rand(6000, 18000));
                        } else {
                            var res = document.createElement('div');
                            res.className = 'cbg-line ' + resCls;
                            res.textContent = result;
                            body.appendChild(res);

                            setTimeout(function () {
                                win.classList.add('cbg-win--out');
                                setTimeout(function () {
                                    if (win.parentNode) win.parentNode.removeChild(win);
                                }, 160);
                            }, rand(700, 2500));
                        }
                    }, 130);
                }
            }, rand(4, 12));
        }, extraDelay || 0);
    }

    function spawnCascade() {
        var n = rand(3, 6);
        var x = rand(3, 22), y = rand(3, 18);
        var stepX = rand(8, 13), stepY = rand(7, 11);
        for (var i = 0; i < n; i++) {
            spawnWindow(i * rand(65, 210), x + i * stepX, y + i * stepY);
        }
    }

    // ── Fatal error popup ───────────────────────────────────────────────────

    function spawnError(posX, posY) {
        var host = getHost();
        if (!host) return;

        var popup = document.createElement('div');
        popup.className = 'cbg-err-popup';
        popup.style.left = (posX !== undefined ? posX : rand(20, 55)) + '%';
        popup.style.top  = (posY !== undefined ? posY : rand(18, 58)) + '%';

        // Layout: [red icon] | [title \n message \n ... \n OK btn]
        var icon = document.createElement('div');
        icon.className = 'cbg-err-popup-icon';
        icon.textContent = '⬛';   // replaced visually via CSS — just a block placeholder
        popup.appendChild(icon);

        var content = document.createElement('div');
        content.className = 'cbg-err-popup-content';

        var titleEl = document.createElement('div');
        titleEl.className = 'cbg-err-popup-title';
        titleEl.textContent = pick(FATAL_TITLES);
        content.appendChild(titleEl);

        var msgEl = document.createElement('div');
        msgEl.className = 'cbg-err-popup-msg';
        msgEl.textContent = pick(FATAL_MSGS);
        content.appendChild(msgEl);

        popup.appendChild(content);

        host.appendChild(popup);

        setTimeout(function () {
            popup.classList.add('cbg-win--out');
            setTimeout(function () {
                if (popup.parentNode) popup.parentNode.removeChild(popup);
            }, 160);
        }, rand(1800, 4000));
    }

    // ── Warning popup ───────────────────────────────────────────────────────

    function spawnWarning(posX, posY) {
        var host = getHost();
        if (!host) return;

        var popup = document.createElement('div');
        popup.className = 'cbg-warn-popup';
        popup.style.left = (posX !== undefined ? posX : rand(15, 60)) + '%';
        popup.style.top  = (posY !== undefined ? posY : rand(15, 60)) + '%';

        var content = document.createElement('div');
        content.className = 'cbg-warn-popup-content';

        var titleEl = document.createElement('div');
        titleEl.className = 'cbg-warn-popup-title';
        titleEl.textContent = pick(WARN_TITLES);
        content.appendChild(titleEl);

        var msgEl = document.createElement('div');
        msgEl.className = 'cbg-warn-popup-msg';
        msgEl.textContent = pick(WARN_MSGS);
        content.appendChild(msgEl);

        popup.appendChild(content);
        host.appendChild(popup);

        setTimeout(function () {
            popup.classList.add('cbg-win--out');
            setTimeout(function () {
                if (popup.parentNode) popup.parentNode.removeChild(popup);
            }, 160);
        }, rand(2500, 5500));
    }

    // ── Scan highlight boxes ────────────────────────────────────────────────

    function spawnScanBox() {
        var host = getHost();
        if (!host) return;
        var n = Math.random() < 0.35 ? rand(2, 4) : 1;
        for (var i = 0; i < n; i++) {
            (function () {
                var glyphVw  = (2 + Math.random() * 3).toFixed(1);
                var boxVw    = (parseFloat(glyphVw) * 1.1).toFixed(2);

                var el = document.createElement('div');
                el.className = 'cbg-scan-box';
                el.style.left   = rand(2, 86) + '%';
                el.style.top    = rand(4, 82) + '%';
                el.style.width  = boxVw + 'vw';
                el.style.height = boxVw + 'vw';

                var glyph = document.createElement('span');
                glyph.textContent = GLYPH_CHARS[Math.floor(Math.random() * GLYPH_CHARS.length)];
                glyph.style.fontSize   = glyphVw + 'vw';
                glyph.style.color      = 'rgba(255,0,51,0.70)';
                glyph.style.fontFamily = 'Courier New, Courier, monospace';
                glyph.style.filter     = 'blur(' + (0.8 + Math.random() * 1.8).toFixed(1) + 'px)';
                el.appendChild(glyph);

                host.appendChild(el);
                setTimeout(function () {
                    el.classList.add('cbg-scan-box--out');
                    setTimeout(function () {
                        if (el.parentNode) el.parentNode.removeChild(el);
                    }, 900);
                }, rand(300, 2000));
            })();
        }
    }

    // ── Floating code fragments ─────────────────────────────────────────────

    function spawnFrag() {
        var host = getHost();
        if (!host) return;

        var el = document.createElement('div');
        el.className = 'cbg-frag';
        el.style.left = rand(2, 82) + '%';
        el.style.top  = rand(3, 82) + '%';
        host.appendChild(el);

        var text = pick(FRAGS);
        var idx  = 0;
        var cur  = '';

        var typeTimer = setInterval(function () {
            if (idx >= text.length) {
                clearInterval(typeTimer);
                setTimeout(function () {
                    el.classList.add('cbg-frag--out');
                    setTimeout(function () {
                        if (el.parentNode) el.parentNode.removeChild(el);
                    }, 360);
                }, rand(18, 75));
                return;
            }
            cur += text[idx++];
            el.textContent = cur;
        }, rand(3, 7));
    }

    // ── Geometry schematic window ────────────────────────────────────────────

    var PHI = (1 + Math.sqrt(5)) / 2;
    var GEO_SHAPES = {
        tetra:  { label:'tetrahedron',  verts:[[1,1,1],[-1,-1,1],[-1,1,-1],[1,-1,-1]], edges:[[0,1],[0,2],[0,3],[1,2],[1,3],[2,3]] },
        cube:   { label:'hexahedron',   verts:[[-1,-1,-1],[1,-1,-1],[1,1,-1],[-1,1,-1],[-1,-1,1],[1,-1,1],[1,1,1],[-1,1,1]], edges:[[0,1],[1,2],[2,3],[3,0],[4,5],[5,6],[6,7],[7,4],[0,4],[1,5],[2,6],[3,7]] },
        octa:   { label:'octahedron',   verts:[[0,1,0],[0,-1,0],[1,0,0],[-1,0,0],[0,0,1],[0,0,-1]], edges:[[0,2],[0,3],[0,4],[0,5],[1,2],[1,3],[1,4],[1,5],[2,4],[4,3],[3,5],[5,2]] },
        icosa:    { label:'icosahedron',     verts:[[0,1,PHI],[0,-1,PHI],[0,1,-PHI],[0,-1,-PHI],[1,PHI,0],[-1,PHI,0],[1,-PHI,0],[-1,-PHI,0],[PHI,0,1],[-PHI,0,1],[PHI,0,-1],[-PHI,0,-1]], edges:[[0,1],[0,4],[0,5],[0,8],[0,9],[1,6],[1,7],[1,8],[1,9],[2,3],[2,4],[2,5],[2,10],[2,11],[3,6],[3,7],[3,10],[3,11],[4,5],[4,8],[4,10],[5,9],[5,11],[6,7],[6,8],[6,10],[7,9],[7,11],[8,10],[9,11]] },
        prism:    { label:'triangular prism',  verts:[[0,1,1],[0.866,-0.5,1],[-0.866,-0.5,1],[0,1,-1],[0.866,-0.5,-1],[-0.866,-0.5,-1]], edges:[[0,1],[1,2],[2,0],[3,4],[4,5],[5,3],[0,3],[1,4],[2,5]] },
        stella:   { label:'stella octangula',  verts:[[1,1,1],[-1,-1,1],[-1,1,-1],[1,-1,-1],[-1,-1,-1],[1,1,-1],[1,-1,1],[-1,1,1]], edges:[[0,1],[0,2],[0,3],[1,2],[1,3],[2,3],[4,5],[4,6],[4,7],[5,6],[5,7],[6,7]] },
        cubocta:  { label:'cuboctahedron',     verts:[[1,1,0],[-1,1,0],[1,-1,0],[-1,-1,0],[1,0,1],[-1,0,1],[1,0,-1],[-1,0,-1],[0,1,1],[0,-1,1],[0,1,-1],[0,-1,-1]], edges:[[0,4],[0,6],[0,8],[0,10],[1,5],[1,7],[1,8],[1,10],[2,4],[2,6],[2,9],[2,11],[3,5],[3,7],[3,9],[3,11],[4,8],[4,9],[5,8],[5,9],[6,10],[6,11],[7,10],[7,11]] },
        antiprism:{ label:'square antiprism',  verts:[[1,1,0],[0,1,1],[-1,1,0],[0,1,-1],[0.707,-1,0.707],[-0.707,-1,0.707],[-0.707,-1,-0.707],[0.707,-1,-0.707]], edges:[[0,1],[1,2],[2,3],[3,0],[4,5],[5,6],[6,7],[7,4],[0,4],[0,7],[1,4],[1,5],[2,5],[2,6],[3,6],[3,7]] },
        pyramid:  { label:'square pyramid',    verts:[[0,1.2,0],[1,-0.5,1],[-1,-0.5,1],[-1,-0.5,-1],[1,-0.5,-1]], edges:[[0,1],[0,2],[0,3],[0,4],[1,2],[2,3],[3,4],[4,1]] },
        pentaprism:{ label:'pentagonal prism', verts:[[1,1,0],[0.309,1,0.951],[-0.809,1,0.588],[-0.809,1,-0.588],[0.309,1,-0.951],[1,-1,0],[0.309,-1,0.951],[-0.809,-1,0.588],[-0.809,-1,-0.588],[0.309,-1,-0.951]], edges:[[0,1],[1,2],[2,3],[3,4],[4,0],[5,6],[6,7],[7,8],[8,9],[9,5],[0,5],[1,6],[2,7],[3,8],[4,9]] },
        dodeca:   { label:'dodecahedron',      verts:[[1,1,1],[1,1,-1],[1,-1,1],[1,-1,-1],[-1,1,1],[-1,1,-1],[-1,-1,1],[-1,-1,-1],[0,0.618,1.618],[0,0.618,-1.618],[0,-0.618,1.618],[0,-0.618,-1.618],[0.618,1.618,0],[0.618,-1.618,0],[-0.618,1.618,0],[-0.618,-1.618,0],[1.618,0,0.618],[1.618,0,-0.618],[-1.618,0,0.618],[-1.618,0,-0.618]], edges:[[0,8],[0,12],[0,16],[1,9],[1,12],[1,17],[2,10],[2,13],[2,16],[3,11],[3,13],[3,17],[4,8],[4,14],[4,18],[5,9],[5,14],[5,19],[6,10],[6,15],[6,18],[7,11],[7,15],[7,19],[8,10],[9,11],[12,14],[13,15],[16,17],[18,19]] }
    };
    var GEO_KEYS = ['tetra','cube','octa','icosa','flower','metatron','prism','stella','cubocta','antiprism','vesica','spiral','lissajous','star5','torus','helix','dodeca','pyramid','pentaprism','rose','cardioid','asteroid','epicycloid','web'];

    // ── Geo-window element report lines ────────────────────────────────────
    var GEO_REPORT_LINES = [
        'Atomic Mass:   [REDACTED]',
        'Isotope Δ:     +0.00441 amu',
        'Valence:       4f¹⁴5d¹⁰6p²',
        'Melting Pt:    3,817°K ± 0.3',
        'Boiling Pt:    [UNSTABLE]',
        'Half-life:     τ = 4.41×10⁻⁷s',
        'Nuclear Spin:  7/2',
        'Isomer State:  m2 confirmed',
        'Bind Energy:   8.812 MeV/nuc',
        'Cross Sect:    σ = 3.301 barn',
        'Decay Chain:   α→β⁻→γ cascade',
        'Oxidation:     +3, +5, +7',
        'Crystal Sys:   Orthorhombic',
        'Density:       19.77 g/cm³',
        'Resistivity:   2.4×10⁻⁸ Ω·m',
        'Band Gap:      0.441 eV',
        'Fermi Level:   4.92 eV',
        'Curie Temp:    1,187 K',
        'Neel Temp:     [classified]',
        'X-Ray Kα:      88.12 keV',
        'Emittance ε:   0.031',
        'Work Fnct φ:   4.41 eV',
        'Plasma ωₚ:     8.8×10¹⁵ Hz',
        'Refract n:     3.301+0.441i',
        'Reflectance:   R = 0.77',
        'Thermal K:     44.1 W/(m·K)',
        'Heat Cp:       28.12 J/(mol·K)',
        'Compressib:    3.3×10⁻¹¹ Pa⁻¹',
        'Synth Method:  Heavy-ion fusion',
        'Discovery:     GLMZ Deep Lab',
        'IUPAC Name:    Unennilium (Φ)',
        'Alt. Name:     [REDACTED]',
        'Registry:      Ψ-441-GLMZ-Φ',
        'Status:        RESTRICTED',
        'Clearance:     CORP EYES ONLY',
        'Sample Mass:   0.0000441 μg',
        'Stability:     [CLASSIFIED]',
        'Yield:         3.301×10⁻¹⁴ %',
        'Hazard:        EXTREME',
        'Manifold:      R⁴ compact',
        'Curvature κ:   +0.00441',
        'Euler χ:       2 (sphere)',
        'Genus:         0 orientable',
        'Betti b₀:      1',
        'Symm Group:    Iₕ (order 120)',
        'Dihedral:      A₅ × Z₂',
        'Vertex deg:    3-regular',
        'Face type:     pentagonal',
        'Dual solid:    icosahedron',
        'Conway:        D(I) notation',
        'Wythoff:       3|2 5',
        'Schläfli:      {5,3}',
        'Vol/Area:      V/A = 0.332',
        'Circum-R:      1.401 norm',
        'Inrad-r:       1.113 norm',
        'Midrad ρ:      1.309 norm',
        'Petrie Poly:   decagon',
        'Dual Verts:    12',
        'Coord Ring:    ℝ[x,y,z]/I₅',
        'Packing η:     0.9069 max',
        'Lattice:       FCC equiv',
        'Point Group:   Oh (48 ops)',
        'Space Group:   Fm3̄m',
        'Fourier k:     2π/a = 1.772',
        'Mode ω₀:       [CLASSIFIED]',
        'Signal SNR:    38.8 dB',
        'Phase δ:       0.441 rad',
        'Resonance:     4,410 Hz',
        'Impedance:     441 + 88i Ω',
    ];

    function spawnGeoWindow() {
        var host = getHost();
        if (!host) return;
        var key = pick(GEO_KEYS);
        var shape = GEO_SHAPES[key];

        var win = document.createElement('div');
        win.className = 'cbg-win cbg-geo-win';
        win.style.left = rand(5, 58) + '%';
        win.style.top  = rand(5, 52) + '%';

        var titleEl = document.createElement('div');
        titleEl.className = 'cbg-title';
        titleEl.textContent = 'schematic/' + (shape ? shape.label : key);
        win.appendChild(titleEl);

        var S = 110;
        var bodyEl = document.createElement('div');
        bodyEl.className = 'cbg-geo-body';
        var cv = document.createElement('canvas');
        cv.className = 'cbg-geo-canvas';
        cv.width = S; cv.height = S;
        bodyEl.appendChild(cv);
        win.appendChild(bodyEl);
        host.appendChild(win);

        var ctx2 = cv.getContext('2d');
        var angle = Math.random() * Math.PI * 2;
        var rafId = null;

        function proj3(v, rx, ry) {
            var x = v[0], y = v[1], z = v[2];
            var cry = Math.cos(ry), sry = Math.sin(ry);
            var x1 = x * cry + z * sry, z1 = -x * sry + z * cry;
            var crx = Math.cos(rx), srx = Math.sin(rx);
            var y1 = y * crx - z1 * srx, z2 = y * srx + z1 * crx;
            var d = 6 / (6 + z2 * 0.5);
            return [x1 * d * 32, y1 * d * 32];
        }

        function drawWire(sh, rx, ry) {
            var pts = sh.verts.map(function(v){ return proj3(v, rx, ry); });
            ctx2.strokeStyle = 'rgba(70,210,170,0.55)'; ctx2.lineWidth = 0.7;
            sh.edges.forEach(function(e){ ctx2.beginPath(); ctx2.moveTo(pts[e[0]][0],pts[e[0]][1]); ctx2.lineTo(pts[e[1]][0],pts[e[1]][1]); ctx2.stroke(); });
            ctx2.fillStyle = 'rgba(70,210,170,0.85)';
            pts.forEach(function(p){ ctx2.beginPath(); ctx2.arc(p[0],p[1],1.2,0,Math.PI*2); ctx2.fill(); });
        }

        function drawFlower(t) {
            var r = 22;
            ctx2.strokeStyle = 'rgba(70,200,220,0.38)'; ctx2.lineWidth = 0.6;
            ctx2.beginPath(); ctx2.arc(0,0,r,0,Math.PI*2); ctx2.stroke();
            for(var i=0;i<6;i++){ var a=(i/6)*Math.PI*2+t*0.05; ctx2.beginPath(); ctx2.arc(Math.cos(a)*r,Math.sin(a)*r,r,0,Math.PI*2); ctx2.stroke(); }
            ctx2.strokeStyle='rgba(70,220,170,0.65)'; ctx2.lineWidth=0.8;
            ctx2.beginPath();
            for(var j=0;j<6;j++){ var a2=(j/6)*Math.PI*2+t*0.05; var p=[Math.cos(a2)*r,Math.sin(a2)*r]; if(j===0)ctx2.moveTo(p[0],p[1]); else ctx2.lineTo(p[0],p[1]); }
            ctx2.closePath(); ctx2.stroke();
        }

        function drawMetatron(t) {
            var r = 16, ir = r, or2 = r * 2;
            var cs = [[0,0]];
            for(var i=0;i<6;i++){ var a=(i/6)*Math.PI*2+t*0.03; cs.push([Math.cos(a)*ir,Math.sin(a)*ir]); }
            for(var i=0;i<6;i++){ var a=(i/6)*Math.PI*2+Math.PI/6+t*0.03; cs.push([Math.cos(a)*or2,Math.sin(a)*or2]); }
            ctx2.strokeStyle='rgba(70,180,220,0.28)'; ctx2.lineWidth=0.5;
            cs.forEach(function(c){ ctx2.beginPath(); ctx2.arc(c[0],c[1],r,0,Math.PI*2); ctx2.stroke(); });
            ctx2.strokeStyle='rgba(70,220,170,0.45)'; ctx2.lineWidth=0.5;
            for(var a=0;a<cs.length;a++) for(var b=a+1;b<cs.length;b++){ ctx2.beginPath(); ctx2.moveTo(cs[a][0],cs[a][1]); ctx2.lineTo(cs[b][0],cs[b][1]); ctx2.stroke(); }
            ctx2.fillStyle='rgba(70,220,170,0.7)';
            cs.forEach(function(c){ ctx2.beginPath(); ctx2.arc(c[0],c[1],1,0,Math.PI*2); ctx2.fill(); });
        }

        function drawVesica(t) {
            var r=24, d=r*0.6;
            ctx2.save(); ctx2.rotate(t*0.02);
            ctx2.strokeStyle='rgba(70,200,220,0.45)'; ctx2.lineWidth=0.7;
            ctx2.beginPath(); ctx2.arc(-d/2,0,r,0,Math.PI*2); ctx2.stroke();
            ctx2.beginPath(); ctx2.arc(d/2,0,r,0,Math.PI*2); ctx2.stroke();
            ctx2.strokeStyle='rgba(70,220,170,0.75)'; ctx2.lineWidth=0.8;
            var half=Math.acos(d/(2*r));
            ctx2.beginPath(); ctx2.arc(-d/2,0,r,-half,half); ctx2.arc(d/2,0,r,Math.PI-half,Math.PI+half); ctx2.closePath(); ctx2.stroke();
            ctx2.restore();
        }
        function drawSpiral(t) {
            var maxR=38, turns=4;
            ctx2.strokeStyle='rgba(70,210,170,0.65)'; ctx2.lineWidth=0.7;
            ctx2.beginPath();
            for(var i=0;i<=300;i++){ var f=i/300; var a=f*turns*Math.PI*2+t*0.05; var r=f*maxR; if(i===0)ctx2.moveTo(Math.cos(a)*r,Math.sin(a)*r); else ctx2.lineTo(Math.cos(a)*r,Math.sin(a)*r); }
            ctx2.stroke();
            ctx2.strokeStyle='rgba(70,190,220,0.35)'; ctx2.lineWidth=0.5;
            ctx2.beginPath();
            for(var i=0;i<=300;i++){ var f=i/300; var a=f*turns*Math.PI*2+t*0.05+Math.PI; var r=f*maxR; if(i===0)ctx2.moveTo(Math.cos(a)*r,Math.sin(a)*r); else ctx2.lineTo(Math.cos(a)*r,Math.sin(a)*r); }
            ctx2.stroke();
        }
        function drawLissajous(t) {
            var a=3, b=2, R=38;
            ctx2.strokeStyle='rgba(70,210,170,0.60)'; ctx2.lineWidth=0.8;
            ctx2.beginPath();
            for(var i=0;i<=400;i++){ var phi=(i/400)*Math.PI*2; var x=R*Math.sin(a*phi+t*0.04); var y=R*Math.sin(b*phi); if(i===0)ctx2.moveTo(x,y); else ctx2.lineTo(x,y); }
            ctx2.stroke();
        }
        function drawStar5(t) {
            var r=36, ir=14, pts=5, rot=t*0.04;
            ctx2.strokeStyle='rgba(70,210,170,0.55)'; ctx2.lineWidth=0.7;
            ctx2.beginPath();
            for(var i=0;i<=pts*2;i++){ var rad=(i%2===0)?r:ir; var a=(i/(pts*2))*Math.PI*2+rot-Math.PI/2; if(i===0)ctx2.moveTo(Math.cos(a)*rad,Math.sin(a)*rad); else ctx2.lineTo(Math.cos(a)*rad,Math.sin(a)*rad); }
            ctx2.closePath(); ctx2.stroke();
            ctx2.strokeStyle='rgba(70,200,220,0.38)'; ctx2.lineWidth=0.6;
            ctx2.beginPath();
            for(var i=0;i<=pts;i++){ var a=(i/pts)*Math.PI*2+rot-Math.PI/2; if(i===0)ctx2.moveTo(Math.cos(a)*ir,Math.sin(a)*ir); else ctx2.lineTo(Math.cos(a)*ir,Math.sin(a)*ir); }
            ctx2.closePath(); ctx2.stroke();
            var op=[]; for(var i=0;i<pts;i++){ var a=(i/pts)*Math.PI*2+rot-Math.PI/2; op.push([Math.cos(a)*r,Math.sin(a)*r]); }
            ctx2.strokeStyle='rgba(70,220,170,0.28)'; ctx2.lineWidth=0.5;
            for(var i=0;i<pts;i++){ var j=(i+2)%pts; ctx2.beginPath(); ctx2.moveTo(op[i][0],op[i][1]); ctx2.lineTo(op[j][0],op[j][1]); ctx2.stroke(); }
        }
        function drawTorus(t) {
            var R=22, r=10, rings=8;
            for(var i=0;i<rings;i++){ var phi=(i/rings)*Math.PI*2+t*0.03; var cx=Math.cos(phi)*R, cy=Math.sin(phi)*R*0.35; var sc=0.5+0.5*Math.abs(Math.cos(phi)); var al=(0.3+0.3*Math.abs(Math.cos(phi))).toFixed(2); ctx2.strokeStyle='rgba(70,210,170,'+al+')'; ctx2.lineWidth=0.6; ctx2.beginPath(); ctx2.ellipse(cx,cy,r*sc,r*0.35,phi,0,Math.PI*2); ctx2.stroke(); }
            ctx2.strokeStyle='rgba(70,200,220,0.45)'; ctx2.lineWidth=0.7;
            ctx2.beginPath(); ctx2.ellipse(0,0,R+r,(R+r)*0.35,0,0,Math.PI*2); ctx2.stroke();
            ctx2.beginPath(); ctx2.ellipse(0,0,R-r,(R-r)*0.35,0,0,Math.PI*2); ctx2.stroke();
        }
        function drawHelix(t) {
            var R=14, turns=3;
            ctx2.lineWidth=0.8;
            ctx2.strokeStyle='rgba(70,210,170,0.65)'; ctx2.beginPath();
            for(var i=0;i<=300;i++){ var f=i/300; var ang=f*turns*Math.PI*2+t*0.05; var y=f*56-28; if(i===0)ctx2.moveTo(Math.cos(ang)*R,y); else ctx2.lineTo(Math.cos(ang)*R,y); }
            ctx2.stroke();
            ctx2.strokeStyle='rgba(70,190,220,0.50)'; ctx2.beginPath();
            for(var i=0;i<=300;i++){ var f=i/300; var ang=f*turns*Math.PI*2+t*0.05+Math.PI; var y=f*56-28; if(i===0)ctx2.moveTo(Math.cos(ang)*R,y); else ctx2.lineTo(Math.cos(ang)*R,y); }
            ctx2.stroke();
            ctx2.strokeStyle='rgba(70,220,170,0.22)'; ctx2.lineWidth=0.5;
            for(var i=0;i<=300;i+=25){ var f=i/300; var ang=f*turns*Math.PI*2+t*0.05; var y=f*56-28; ctx2.beginPath(); ctx2.moveTo(Math.cos(ang)*R,y); ctx2.lineTo(Math.cos(ang+Math.PI)*R,y); ctx2.stroke(); }
        }

        function drawRose(t) {
            var R=38, k=3;
            ctx2.strokeStyle='rgba(70,210,170,0.70)'; ctx2.lineWidth=0.8;
            ctx2.beginPath();
            for(var i=0;i<=720;i++){
                var th=(i/720)*Math.PI*2;
                var r=R*Math.cos(k*(th+t*0.018));
                if(i===0) ctx2.moveTo(r*Math.cos(th),r*Math.sin(th));
                else ctx2.lineTo(r*Math.cos(th),r*Math.sin(th));
            }
            ctx2.stroke();
            ctx2.strokeStyle='rgba(70,200,220,0.22)'; ctx2.lineWidth=0.4;
            ctx2.beginPath(); ctx2.arc(0,0,R,0,Math.PI*2); ctx2.stroke();
        }
        function drawCardioid(t) {
            var a=18, rot=t*0.012;
            ctx2.strokeStyle='rgba(70,210,170,0.65)'; ctx2.lineWidth=0.8;
            ctx2.beginPath();
            for(var i=0;i<=360;i++){
                var th=(i/360)*Math.PI*2;
                var r=a*(1+Math.cos(th));
                if(i===0) ctx2.moveTo(r*Math.cos(th+rot),r*Math.sin(th+rot));
                else ctx2.lineTo(r*Math.cos(th+rot),r*Math.sin(th+rot));
            }
            ctx2.stroke();
            ctx2.strokeStyle='rgba(70,200,220,0.22)'; ctx2.lineWidth=0.5;
            ctx2.beginPath(); ctx2.arc(0,0,a*2,0,Math.PI*2); ctx2.stroke();
        }
        function drawAsteroid(t) {
            var R=36, rot=t*0.02;
            ctx2.strokeStyle='rgba(70,210,170,0.65)'; ctx2.lineWidth=0.8;
            ctx2.beginPath();
            for(var i=0;i<=360;i++){
                var p=(i/360)*Math.PI*2;
                var x=R*Math.pow(Math.cos(p+rot),3), y=R*Math.pow(Math.sin(p+rot),3);
                if(i===0) ctx2.moveTo(x,y); else ctx2.lineTo(x,y);
            }
            ctx2.stroke();
            ctx2.strokeStyle='rgba(70,200,220,0.25)'; ctx2.lineWidth=0.5;
            ctx2.beginPath(); ctx2.arc(0,0,R,0,Math.PI*2); ctx2.stroke();
            for(var i=0;i<4;i++){
                var a=(i/4)*Math.PI*2+rot;
                ctx2.strokeStyle='rgba(70,220,170,0.18)'; ctx2.lineWidth=0.4;
                ctx2.beginPath(); ctx2.moveTo(0,0); ctx2.lineTo(Math.cos(a)*R,Math.sin(a)*R); ctx2.stroke();
            }
        }
        function drawEpicycloid(t) {
            var Rc=24, rc=8, rot=t*0.015;
            ctx2.strokeStyle='rgba(70,210,170,0.65)'; ctx2.lineWidth=0.8;
            ctx2.beginPath();
            for(var i=0;i<=600;i++){
                var p=(i/600)*Math.PI*2+rot;
                var x=(Rc+rc)*Math.cos(p)-rc*Math.cos((Rc/rc+1)*p);
                var y=(Rc+rc)*Math.sin(p)-rc*Math.sin((Rc/rc+1)*p);
                if(i===0) ctx2.moveTo(x,y); else ctx2.lineTo(x,y);
            }
            ctx2.stroke();
            ctx2.strokeStyle='rgba(70,200,220,0.22)'; ctx2.lineWidth=0.5;
            ctx2.beginPath(); ctx2.arc(0,0,Rc,0,Math.PI*2); ctx2.stroke();
        }
        function drawWeb(t) {
            var rings=5, spokes=8, maxR=40, rot=t*0.01;
            ctx2.strokeStyle='rgba(70,200,220,0.35)'; ctx2.lineWidth=0.5;
            for(var s=0;s<spokes;s++){
                var a=(s/spokes)*Math.PI*2+rot;
                ctx2.beginPath(); ctx2.moveTo(0,0); ctx2.lineTo(Math.cos(a)*maxR,Math.sin(a)*maxR); ctx2.stroke();
            }
            ctx2.strokeStyle='rgba(70,210,170,0.55)'; ctx2.lineWidth=0.7;
            for(var ri=1;ri<=rings;ri++){
                var r=(ri/rings)*maxR;
                ctx2.beginPath();
                for(var s=0;s<=spokes;s++){
                    var a=(s/spokes)*Math.PI*2+rot;
                    if(s===0) ctx2.moveTo(Math.cos(a)*r,Math.sin(a)*r);
                    else ctx2.lineTo(Math.cos(a)*r,Math.sin(a)*r);
                }
                ctx2.closePath(); ctx2.stroke();
            }
        }

        function geoFrame() {
            ctx2.clearRect(0,0,S,S);
            ctx2.save(); ctx2.translate(S/2,S/2);
            angle += 0.007;
            if      (key==='flower')     drawFlower(angle);
            else if (key==='metatron')   drawMetatron(angle);
            else if (key==='vesica')     drawVesica(angle);
            else if (key==='spiral')     drawSpiral(angle);
            else if (key==='lissajous')  drawLissajous(angle);
            else if (key==='star5')      drawStar5(angle);
            else if (key==='torus')      drawTorus(angle);
            else if (key==='helix')      drawHelix(angle);
            else if (key==='rose')       drawRose(angle);
            else if (key==='cardioid')   drawCardioid(angle);
            else if (key==='asteroid')   drawAsteroid(angle);
            else if (key==='epicycloid') drawEpicycloid(angle);
            else if (key==='web')        drawWeb(angle);
            else                         drawWire(shape, angle*0.4, angle);
            ctx2.restore();
            rafId = requestAnimationFrame(geoFrame);
        }
        geoFrame();

        // ── Scrolling element report text (right of canvas) ────────────────
        var reportEl = document.createElement('div');
        reportEl.className = 'cbg-geo-report';
        var innerEl = document.createElement('div');
        innerEl.className = 'cbg-geo-report-inner';
        reportEl.appendChild(innerEl);
        bodyEl.appendChild(reportEl);

        var lineH = 9;
        var visCount = Math.ceil(110 / lineH) + 3;
        var reportBuf = [];
        for (var ri = 0; ri < visCount + 2; ri++) { reportBuf.push(pick(GEO_REPORT_LINES)); }
        innerEl.textContent = reportBuf.join('\n');
        var scrollOff = 0;
        var scrollTmr = setInterval(function () {
            scrollOff += 0.35;
            if (scrollOff >= lineH) {
                scrollOff -= lineH;
                reportBuf.shift();
                reportBuf.push(pick(GEO_REPORT_LINES));
                innerEl.textContent = reportBuf.join('\n');
            }
            innerEl.style.transform = 'translateY(' + scrollOff.toFixed(1) + 'px)';
        }, 60);

        var ttl = rand(4000, 9000);
        setTimeout(function () {
            clearInterval(scrollTmr);
            cancelAnimationFrame(rafId);
            win.classList.add('cbg-win--out');
            setTimeout(function () { if (win.parentNode) win.parentNode.removeChild(win); }, 500);
        }, ttl);
    }

    // ── Corporate memo intercept ──────────────────────────────────────────────

    var MEMOS = [
        'CLASSIFIED // EYES ONLY\nTO: VP Asset Security\nFROM: Board Dir. 94-C\nRE: Unit 7-W — Liability\n\nProceed w/ scheduled termination.\nFull blackout per Protocol 9.\nNo media. No record. Confirm 0300.\nAuth: 0x9f3a\n\n>>> GLMZ RELAY INTERCEPT <<<',
        'INTERNAL — DELETE AFTER READ\nTO: District Enforcement\nRE: Freelancer File #3301\n\nFlag for immediate termination.\nAssociates: Annex B.\nOffer suppress — THEN terminate.\nSilent bounty: Φ40,000 corp rate.\n\n>>> INTERCEPT :: dist12/relay <<<',
        'FROM: Crest Dynamics Legal\nTO: Subsidiary Compliance\nRE: Neural Data Harvest — Batch 7\n\nHarvest neural-sig per Q3 plan.\nConsent waiver buried in ToS §44-F.\nDo NOT flag to district oversight.\nDelete harvest logs after 72h.\n\n[RELAY CORRUPTION — partial recv]',
        'PRIORITY: URGENT\nTO: Security Chief, Dist 9\nRE: Witness Mgmt — Case #0091\n\nAll three witnesses — silence them.\nPreferred: reassignment.\nFallback: Protocol Null.\nContractor already briefed.\n\n[SIG: 0xdc3545 // VAULTDROP]',
        'FROM: Acquisition Strategy\nTO: Field Operations\nRE: Sector 4 Hostile Takeover\n\nPhase 1: Destroy competitor supply.\nPhase 2: Corner remaining market.\nPhase 3: Price floor +400%.\nKeep district enforcement on payroll.\n\n>>> DARK NODE :: glmz/7 <<<',
        'TO: Behavioral Analytics Team\nRE: Pop. Compliance — Batch 9\n\nDeploy sublim. seq. in dist2 feed.\nTarget: dissent suppression.\nVector: entertainment network.\nDeny if queried. Log: none.\n\n[INTERCEPT CONFIDENCE: 0.88]',
        'PRIVILEGED COMMUNICATION\nFROM: Acquisitions Div. 3\nTO: Field Security\nRE: Competitor Asset — Terminal\n\nSubject refused acquisition offer.\nAuthorize final resolution.\nRecover IP before cleanup.\nRoute via dark node only.\n\n[GLMZ INTERCEPT: 94%]',
        'FROM: Behavioral Mod. Group\nTO: Neural Interface Program\nRE: Opt-Out Handling — Priority\n\nOperators flagging consent removal:\nDo NOT honor. Flag as compromised.\nAccelerate neural-key binding.\nLegal has pre-approved language.\n\n>>> CREST DYNAMICS INTERNAL <<<',
        'URGENT — ALL DISTRICT COMMANDERS\nFROM: Enforcement Central\nRE: Freelancer Surge — District 7\n\nSurge is cover. Target is #3301.\nCollateral: acceptable up to 40%.\nNo press. No GLMZ incident report.\nClose window before 0600.\n\n[AUTH: 0xBE0441]',
        'FROM: Data Harvesting Unit 9\nTO: Board — Eyes Only\nRE: Q3 Neural Data — Profit\n\n44,000 operator profiles sold.\nBuyer: NeuralState consortium.\nData stripped of ID — plausibly.\nReturn: Φ4.4M corp rate.\n\n>>> RELAY CORRUPTION <<<',
        'TO: Corp Liaison, District 9\nFROM: Legal Stratagem\nRE: Liability Suppression\n\nSeven incident reports — suppress.\nSettle: Φ3,000 each, no admission.\nIf refused: standard protocol 7.\nDestroy original filings after.\n\n[INTERCEPT: dist9/dark-node]',
        'FROM: Medical Ethics Bypass\nTO: Cyberware Division\nRE: Non-Consenting Subjects\n\nTrial cohort 44 is involuntary.\nClinical oversight circumvented.\nOutcomes trending positive.\nTerminate failed cohort quietly.\n\n>>> DARK NODE :: dist4 <<<',
        'CLASSIFIED — BOARD LEVEL ONLY\nFROM: Asset Liquidation\nTO: Director 94-C\nRE: Witness List — Case #0099\n\nSix witnesses. Five located.\nSchedule: sequential, 72h window.\nMake them look accidental.\nFinal — offer Φ80k, else same.\n\n[SIG: 0x9A3301 // VERIFIED]',
        'FROM: Media Relations (Covert)\nTO: Entertainment Network\nRE: Narrative Seeding — Phase 3\n\nInsert: freelancers = terrorists.\nEnforcement = civic guardians.\nSubtlety required — 6mo campaign.\nDeny corp authorship at all costs.\n\n[RELAY: glmz-comms-d12]',
        'TO: Corp Security, District 4\nFROM: Territorial Division\nRE: Hostile Freelancer — 0x4492\n\nOperator has evidence of Q3 harvest.\nContain before they reach press.\nLevel 3 protocol authorized.\nNo record of this transmission.\n\n>>> RELAY CORRUPTION — partial <<<',
        'FROM: Population Control Div.\nTO: GLMZ District Administrators\nRE: Food Access Throttle — Batch 4\n\nDistrict 9 rationing at 44%.\nReduction to 30% approved Q1.\nFrame as supply chain failure.\nTrack compliance via BCI metrics.\n\n[INTERCEPT CONFIDENCE: 0.91]',
        'INTERNAL — DESTROY AFTER READ\nFROM: Corp Ethics Committee\nTO: [REDACTED]\nRE: Upcoming Ethics Review\n\nAnswers to inquiries 3, 7, 12:\n— Data: "aggregated, anonymized"\n— Consent: "implied via ToS"\n— Deaths: "within projections"\nMembers have been briefed.\n\n[SIG: 0xDEAD4412]',
        // 15 more
        'FROM: Extraction Unit 7\nTO: Field Ops Director\nRE: Asset 3301 — Status Update\n\nAsset uncooperative after 72h.\nStandard persuasion ineffective.\nPhase 2 authorized by Dir. 94-C.\nDispose after extraction complete.\n\n>>> GLMZ DARK NODE :: dist4 <<<',
        'FROM: Neural Analytics Board\nTO: BCI Program Dir.\nRE: Operator 0x4492 — Classify\n\nNeural profile matches dissident tag.\nRecommend silent reclassification.\nAccess throttle: 40% — covert.\nDo not inform subject.\n\n[RELAY: sec/vault-7]',
        'CONFIDENTIAL — NO EXTERNAL\nFROM: Subsidiary Relations\nTO: Enforcement Liaison\nRE: Competitor Infrastructure\n\nThree relay nodes confirmed hostile.\nCoordinate with dist9 enforcement.\nPlausible denial required.\nNo Crest equipment — freelancers.\n\n>>> INTERCEPT :: corp/mirror <<<',
        'TO: Narrative Ops Team\nFROM: Social Influence Div.\nRE: Freelancer Problem — Framing\n\nCurrent narrative: economic threat.\nProposed pivot: public safety.\nTimeline: 3-week push via ent/net.\nSuccess metric: 60% public favor.\n\n[GLMZ RELAY: dist12/comms]',
        'INTERNAL ONLY — LEGAL\nFROM: Compliance Division\nTO: Security Dir.\nRE: Incident 0091 — Paperwork\n\nThree deaths out of scope.\nFile as industrial accident.\nFamily settlements: Φ8,000 each.\nNDA required — enforce aggressively.\n\n[SIG: 0x9A3301 // CORP-LEGAL]',
        'FROM: Behavioral Modification R&D\nTO: Program Board\nRE: Trial Cohort 7 — Outcomes\n\nCompliance rate: 88% (target 80%).\nSubjects unaware of BCI seeding.\nSide effects: within tolerance.\nProceed to Batch 8 — 500 subjects.\n\n>>> DARK NODE :: dist7/relay <<<',
        'URGENT PRIORITY\nFROM: Intelligence Operations\nTO: Crest Dynamics\nRE: Journalist — Case #0441\n\nJournalist has partial Q3 data.\nSourced from inside — find leak.\nContain story before press cycle.\nPermanent solution if necessary.\n\n[INTERCEPT CONFIDENCE: 0.96]',
        'FROM: Territorial Expansion\nTO: Legal + Enforcement\nRE: District 2 Consolidation\n\nPhase 1 complete: 3 orgs dissolved.\nPhase 2: Purchase remaining assets.\nPhase 3: Restructure workforce.\nExpected redundancies: 400-600.\n\n>>> CORP/BROKER :: VAULTDROP <<<',
        'CLASSIFIED — ABOVE TOP\nFROM: AI Autonomy Division\nTO: Board Only\nRE: Behemoth Meridian-88 — Update\n\nAutonomy module fully deployed.\nHuman oversight: symbolic only.\nContingency removal: scheduled.\nDo not log this meeting.\n\n[SIG: 0xFF0000 // PURGE-ON-READ]',
        'TO: Forensic Suppression Team\nFROM: Director 94-C\nRE: Evidence — Batch 0099\n\nSeven files. Delete originals.\nOverwrite media 3 passes.\nPurge relay cache: dist4, dist7.\nConfirm by 0300 UTC.\n\n>>> RELAY CORRUPTION — terminal <<<',
        'FROM: Public Health Proxy\nTO: District Supply Chain\nRE: Pharmaceutical Diversion\n\nDivert Batch 44 to compliance stream.\nReduce district 9 access 60%.\nFrame as shortage — corp approved.\nProfits to hidden account 0x9F3A.\n\n[INTERCEPT: glmz/gate-12]',
        'INTERNAL — NO EXTERNAL\nFROM: BCI Surveillance Unit\nTO: Corp Intelligence\nRE: Watchlist Update — Q2\n\n3,301 operators under passive monitor.\n412 flagged for attention.\nNeural-key patterns attached.\nAutomated escalation if triggered.\n\n>>> CREST INTERNAL :: sec/enclave <<<',
        'TO: Field Security, District 12\nFROM: Asset Protection\nRE: Freelancer Collective — Action\n\nCell identified: 4 members, dark-node.\nSurveillance complete: 18 days.\nAuthorize simultaneous termination.\nCoordinate with dist9 — 0400.\n\n[SIG: 0xBE0441 // SILENT]',
        'FROM: Data Monetization Group\nTO: Board of Directors\nRE: Neural Signature Auction — Q3\n\n88,000 unique profiles ready.\nSale to NeuralState: Φ8.8M.\nAnonymization: cosmetic only.\nAudit-proof — legal reviewed.\n\n>>> RELAY: corp/mirror-d7 <<<',
        'PRIORITY ALPHA\nFROM: Enforcement Central\nTO: ALL DISTRICT COMMANDS\nRE: Operation Dark Census\n\nRound up all unregistered operators.\nBCI registration mandatory by 0600.\nNon-compliance: level 3 detention.\nMedia blackout in effect.\n\n[RELAY CORRUPTION — CRITICAL]',
    ];

    // ── Warning popup data ──────────────────────────────────────────────────
    var WARN_TITLES = [
        'WARN — latency spike',     'WARN — retry limit',       'WARN — signal degraded',
        'WARN — disk 85% full',     'WARN — cert expires 7d',   'WARN — audit gap',
        'WARN — bci drift',         'WARN — temp elevated',     'WARN — corp deviation',
        'WARN — foreign pattern',   'WARN — rate limit near',   'WARN — replica lag',
        'WARN — operator anomaly',  'WARN — entropy low',       'WARN — auth failures',
        'WARN — memory pressure',   'WARN — port saturation',   'WARN — beacon change',
        'WARN — key rotation due',  'WARN — zone access freq',  'WARN — crest wear score',
        'WARN — hkb fluid low',     'WARN — net tunnel flap',   'WARN — db stats stale',
        'NOTICE — process flagged', 'NOTICE — unusual traffic', 'NOTICE — policy deviation',
        'NOTICE — shadow process',  'NOTICE — off-hours access','NOTICE — location mismatch',
        'NOTICE — glmz gate usage', 'NOTICE — schedule deviate',
    ];

    var WARN_MSGS = [
        'Operator 0x3301 accessed zone B\n7 events in 24h — baseline is 2',
        'BCI latency 18ms — threshold 10ms\nClassifier confidence 0.71 — watch',
        'Cert glmz.relay.internal: 7d expiry\nRenewal scheduled — no action yet',
        'Disk /var/bci at 85% capacity\nSchedule cleanup — 2h to critical',
        'Replication lag 4.4s — watch\nPrimary write rate elevated 3×',
        'Entropy pool: 48 bits — low\nReseeding from RDRAND — ok for now',
        'HKB wear_score=0.72 — service soon\n88 cycles to recommended interval',
        'BCI drift: 0.8μV/min on ring2\nBelow alarm threshold — log started',
        'Operator 0x7712 off-hours access\n03:22 district4 — within policy',
        'Net beacon 0xBE04 freq change\n+12kHz shift — interference or move?',
        'Corp audit: 4 events out-of-order\nTimestamps within 200ms — clock skew?',
        'Auth failures uid=3301: 3 in 1h\nAbove baseline 1 — no lockout yet',
        'Memory RSS 1.4G — 73% of limit\nGrowth 44MB/h — check for leak',
        'glmz gate 12 use 44× today\nBaseline 12× — freelancer surge?',
        'Shadow process pid=31337 found\nNot in manifest — investigating',
        'Location mismatch: 0x4492\nLast seen d7, login from d12',
        'Corp policy deviation: uid=0x3301\n3 zone-B accesses without prior auth',
        'Foreign key pattern in BCI stream\nClassifier flagging unknown operator',
        'Key rotation 12d overdue: corp-vault\nSchedule window before expiry',
        'RBS thermal 47C — within spec\nElev. above baseline 38C — log',
        'HKB hydraulic: fluid 20% remaining\nRefill due before next session',
        'Net tunnel wg to dist4 flapped 3×\nStability: 94% uptime — watch',
        'DB stats 180s stale: bci_events\nPlanner using old data — ANALYZE',
        'Cert corp-enclave depth=1: 14d\nAuto-renew queued — monitor',
        'BCI ring3 impedance creeping up\n18kΩ — trend toward 22kΩ limit',
        // 30 more
        'RBS disc wear-score 0.61 — service at 0.80\n14,200 rotations logged this quarter',
        'BCI ring2 noise floor elevated\nBaseline 0.8μV → now 1.4μV — possible impedance',
        'HKB spring tension 87% spec\nFall below 80% triggers manual inspect',
        'Enclave attestation skew 3.2s\nClock drift — resync before next audit',
        'GLMZ node dist12 latency 88ms\nBaseline 22ms — check relay path',
        'Corp sync drift: 9 events behind\nPrimary load elevated — catch-up queued',
        'Auth token expiry in 4h: uid=0x4492\nRenewal queued — no action needed yet',
        'Relay packet loss 0.8%: dist9→dist12\nAbove baseline 0.1% — path degraded',
        'DB write amplification 4.8×\nLSM compaction behind — schedule window',
        'File descriptors 1800/2048\nApproaching limit — check for leaks',
        'CPU core3 throttling at 88°C\nCooling nominal — sustained load spike',
        'Swap pressure 14% — watch\nRSS trending up, GC may not keep pace',
        'TLS cert chain depth=4: corp-root\nUnusual — verify issuer hierarchy',
        'BCI motor cortex drift 0.6%/hr\nBelow abort threshold — calibrate next session',
        'Dark-node latency 340ms: glmz/7\nNominal <80ms — relay path congested',
        'Audit log 44s behind real-time\nWriter blocked on disk flush — ok for now',
        'Snapshot 48h stale: bci_events\nScheduled refresh missed — retry queued',
        'License utilization 88%: bci_nodes\n12 seats remain — notify procurement',
        'RNG entropy pool 32 bits\nBelow 64-bit threshold — kernel reseed pending',
        'BCI power reserve 12%\nBattery degradation flag — replace before next op',
        'IPC queue depth 420: bci_daemon\nNominal 50 — consumer falling behind',
        'HKB cycle count 9,800 — service at 10k\nSchedule maintenance before next session',
        'Corp enforcement notice: zone-C access\nOperator 0x3301 flagged — within policy',
        'Corp data retention horizon breach\nEvents older than 90d present — archive',
        'Corp enclave query volume spike\n3× baseline in last hour — investigate',
        'Off-site login: uid=0x7712 dist4\nLast known location: dist9 — review',
        'BCI key age 180d: uid=0x3301\nRotation overdue — schedule in 14d window',
        'Rate limit 90%: relay forwarding\nCorp quota — request increase or throttle',
        'New district node dist2/fab-9 online\nNot in routing table — manual review',
        'Freelancer 0x4492 on watchlist match\nLow confidence — monitor, no action yet',
    ];

    // ── Glyph character pool (scan-box content) ─────────────────────────────
    var GLYPH_CHARS =
        '░▒▓█▌▐▀▄■□◆◇○●◉◎' +
        '∞≠≈∫∂∆Ωπμλφψξζ⊗⊕⊙∅' +
        '←→↑↓↔↕⇐⇒⇑⇓' +
        '₿€¥₩₹Φ' +
        '✦✧✩✫✭✯✱✲✳✴✵✶✷✸' +
        'ΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣΤΥΦΧΨΩ' +
        'αβγδεζηθικλμνξοπρστυφχψω';

    function eraseMemo(el) {
        var BLOCK = '█▓▒░▪■▫';
        var chars = el.textContent.split('');
        var total = chars.length;
        var phase = 0;
        var count = 0;
        var timer = setInterval(function () {
            var n = rand(4, 10);
            for (var i = 0; i < n; i++) {
                var idx = rand(0, total - 1);
                if (chars[idx] === '\n') continue;
                if (phase === 0) {
                    chars[idx] = BLOCK[rand(0, BLOCK.length - 1)];
                } else {
                    chars[idx] = ' ';
                }
            }
            el.textContent = chars.join('');
            count += n;
            if (phase === 0 && count >= total * 1.6) { phase = 1; count = 0; }
            if (phase === 1 && count >= total * 1.6) {
                clearInterval(timer);
                if (el.parentNode) el.parentNode.removeChild(el);
            }
        }, 22);
    }

    function spawnMemo() {
        var host = getHost();
        if (!host) return;
        var el = document.createElement('div');
        el.className = 'cbg-memo';
        el.style.left = rand(6, 52) + '%';
        el.style.top  = rand(6, 52) + '%';
        el.textContent = pick(MEMOS);
        host.appendChild(el);
        setTimeout(function () { eraseMemo(el); }, rand(3000, 6000));
    }

    // ── Tick loop ───────────────────────────────────────────────────────────

    function spawnCascadeError() {
        var n = rand(2, 4);
        var x = rand(10, 30), y = rand(8, 22);
        var stepX = rand(9, 14), stepY = rand(8, 13);
        for (var i = 0; i < n; i++) {
            (function (delay, px, py) {
                setTimeout(function () { spawnError(px, py); }, delay);
            })(i * rand(120, 320), x + i * stepX, y + i * stepY);
        }
    }

    function tick() {
        if (!getHost()) { tickTimer = null; return; }
        var r = Math.random();
        if      (r < 0.10) spawnError();
        else if (r < 0.18) spawnWarning();
        else if (r < 0.22) spawnMemo();
        else if (r < 0.30) spawnScanBox();
        else if (r < 0.42) spawnGeoWindow();
        else if (r < 0.47) spawnCascade();
        else if (r < 0.61) spawnFrag();
        else                spawnWindow();
        tickTimer = setTimeout(tick, rand(900, 3200));
    }

    // ── Quake lightstyle scan lines ─────────────────────────────────────────
    // Each char 'a'–'z': brightness = (charCode-97)/12.  'm'=1.0 (normal).
    // Two layers run at slightly different frame rates — they never phase-lock.
    // Reference styles from id Software Quake engine source (1996):
    //   style  1: "mmnmmommommnonmmonqnmmo"         (flicker)
    //   style 10: "mmamammmmammamamaaamammma"        (fluorescent flicker)

    var SL_FINE_STYLE   = 'mmnmmommommnonmmonqnmmo';
    var SL_COARSE_STYLE = 'mmamammmmammamamaaamammma';
    var SL_FINE_MAX     = 0.30;
    var SL_COARSE_MAX   = 0.22;

    var slFineIdx = 0, slCoarseIdx = 0;
    var slFineTimer = null, slCoarseTimer = null;

    function slStep(style, idx) {
        return (style.charCodeAt(idx) - 97) / 12;
    }

    function startScanLines() {
        if (slFineTimer)   { clearInterval(slFineTimer);   slFineTimer = null; }
        if (slCoarseTimer) { clearInterval(slCoarseTimer); slCoarseTimer = null; }

        slFineTimer = setInterval(function () {
            var el = document.querySelector('.cbg-sl-fine');
            if (!el) { clearInterval(slFineTimer); slFineTimer = null; return; }
            el.style.opacity = slStep(SL_FINE_STYLE, slFineIdx) * SL_FINE_MAX;
            slFineIdx = (slFineIdx + 1) % SL_FINE_STYLE.length;
        }, 100);

        slCoarseTimer = setInterval(function () {
            var el = document.querySelector('.cbg-sl-coarse');
            if (!el) { clearInterval(slCoarseTimer); slCoarseTimer = null; return; }
            el.style.opacity = slStep(SL_COARSE_STYLE, slCoarseIdx) * SL_COARSE_MAX;
            slCoarseIdx = (slCoarseIdx + 1) % SL_COARSE_STYLE.length;
        }, 107);
    }

    function stopScanLines() {
        if (slFineTimer)   { clearInterval(slFineTimer);   slFineTimer = null; }
        if (slCoarseTimer) { clearInterval(slCoarseTimer); slCoarseTimer = null; }
        var f = document.querySelector('.cbg-sl-fine');
        var c = document.querySelector('.cbg-sl-coarse');
        if (f) f.style.opacity = '0';
        if (c) c.style.opacity = '0';
    }

    function start() {
        if (tickTimer) return;  // already running — preserve state across navigations
        if (getHost()) {
            tickTimer = setTimeout(tick, rand(500, 1500));
            startScanLines();
        }
    }

    document.addEventListener('blazor:navigated', start);

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', start);
    } else {
        start();
    }

    return { start: start };
})();
