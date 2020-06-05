package main

import (
	"context"
	"fmt"
	"os"
	"path"

	apicorev1 "k8s.io/api/core/v1"
	metav1 "k8s.io/apimachinery/pkg/apis/meta/v1"
	"k8s.io/client-go/kubernetes"
	v1 "k8s.io/client-go/kubernetes/typed/core/v1"
	"k8s.io/client-go/rest"
	"k8s.io/client-go/tools/clientcmd"
	// core
	// "k8s.io/apimachinery/pkg/api/errors"
	// metav1 "k8s.io/apimachinery/pkg/apis/meta/v1"
	// "k8s.io/client-go/kubernetes"
	// "k8s.io/client-go/rest"
	//
	// Uncomment to load all auth plugins
	// _ "k8s.io/client-go/plugin/pkg/client/auth"
	//
	// Or uncomment to load specific auth plugins
	// _ "k8s.io/client-go/plugin/pkg/client/auth/azure"
	// _ "k8s.io/client-go/plugin/pkg/client/auth/gcp"
	// _ "k8s.io/client-go/plugin/pkg/client/auth/oidc"
	// _ "k8s.io/client-go/plugin/pkg/client/auth/openstack"
)

func getConfig() (*rest.Config, error) {
	var config *rest.Config
	var err error

	inPod := os.Getenv("KUBERNETES_SERVICE_HOST") != ""
	if inPod {
		config, err = rest.InClusterConfig()
	} else {

		// uses the current context in kubeconfig
		// path-to-kubeconfig -- for example, /root/.kube/config
		homeDir, err := os.UserHomeDir()
		if err != nil {
			return nil, err
		}

		configPath := path.Join(homeDir, ".kube", "config")
		config, err = clientcmd.BuildConfigFromFlags("", configPath)
	}

	return config, err
}

func getCoreV1() (v1.CoreV1Interface, error) {
	config, err := getConfig()
	if err != nil {
		return nil, err
	}

	// creates the clientset
	clientset, err := kubernetes.NewForConfig(config)
	if err != nil {
		return nil, err
	}

	// access the API to list pods
	coreV1 := clientset.CoreV1()
	return coreV1, nil
	//pods, _ := clientset.CoreV1().Pods("").List(v1.ListOptions{})
}

func ensureEphemeralContainer(
	namespace string,
	podName string,
	containerName string,
	imageName string,
	debug bool) error {
	v1Client, err := getCoreV1()
	if err != nil {
		return err
	}

	// Get the pod
	fmt.Printf("Getting %v from %v\n", podName, namespace)
	pod, err := v1Client.Pods(namespace).Get(context.TODO(), podName, metav1.GetOptions{})
	fmt.Printf("Retrieved pod: %v\n", pod.Name)

	ec := apicorev1.EphemeralContainer{}
	ec.Name = containerName
	ec.Image = imageName
	// Should likely switch to Always for security reasons
	ec.ImagePullPolicy = "IfNotPresent"
	ec.TerminationMessagePolicy = "File"
	ec.Stdin = debug
	ec.TTY = debug
	// TODO: this should come from a config / param
	ec.VolumeMounts = []apicorev1.VolumeMount{
		{
			Name:      "work-dir",
			MountPath: "/actions-runner/_work",
		}}
	// ec.VolumeDevices = []apicorev1.VolumeDevice{
	// 	{
	// 		Name:       "work-dir",
	// 		DevicePath: "/actions-runner/_work",
	// 	}}

	// https://kubernetes.io/docs/concepts/workloads/pods/ephemeral-containers/#ephemeral-containers-api
	ecSubRes := apicorev1.EphemeralContainers{}
	ecSubRes.APIVersion = "v1"
	ecSubRes.Kind = "EphemeralContainers"
	ecSubRes.ObjectMeta.Name = podName
	ecSubRes.EphemeralContainers = append(pod.Spec.EphemeralContainers, ec)

	fmt.Printf("%v\n", ecSubRes)

	// hmmm, pod.Name is empty inside the pod.  use the one passed
	fmt.Printf("Updating pod %v\n", podName)
	_, err = v1Client.Pods(namespace).UpdateEphemeralContainers(context.TODO(), podName, &ecSubRes, metav1.UpdateOptions{})
	if err != nil {
		return err
	}

	return nil
}
